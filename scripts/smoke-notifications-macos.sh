#!/usr/bin/env bash

set -u

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_PROJECT="$ROOT_DIR/src/NotifsTestApp"
GHOSTTY_CONFIG="$HOME/Library/Application Support/com.mitchellh.ghostty/config"

GREEN="\033[0;32m"
YELLOW="\033[0;33m"
RED="\033[0;31m"
BLUE="\033[0;34m"
NC="\033[0m"

PASSED=0
FAILED=0
MANUAL_PROMPTS=0
PRE_SEND_SLEEP_SECONDS=5

print_header() {
  printf "\n${BLUE}== %s ==${NC}\n" "$1"
}

pass() {
  PASSED=$((PASSED + 1))
  printf "${GREEN}PASS${NC} %s\n" "$1"
}

fail() {
  FAILED=$((FAILED + 1))
  printf "${RED}FAIL${NC} %s\n" "$1"
}

info() {
  printf "${BLUE}INFO${NC} %s\n" "$1"
}

unique_message() {
  local label="$1"
  printf "%s | pid=%s | ts=%s | rand=%s" "$label" "$$" "$(date +%H:%M:%S)" "$RANDOM"
}

run_cmd() {
  local name="$1"
  shift

  printf -- "- %s\n" "$name"
  if "$@"; then
    pass "$name"
  else
    fail "$name"
  fi
}

run_expect_zero() {
  local name="$1"
  shift

  printf -- "- %s\n" "$name"
  if "$@" >/dev/null 2>&1; then
    pass "$name"
  else
    fail "$name"
  fi
}

show_env() {
  print_header "Environment"
  printf "TERM_PROGRAM=%s\n" "${TERM_PROGRAM:-<null>}"
  printf "TERM=%s\n" "${TERM:-<null>}"
  printf "WT_SESSION=%s\n" "${WT_SESSION:-<null>}"
}

check_ghostty_config() {
  print_header "Ghostty Config"

  if [[ -f "$GHOSTTY_CONFIG" ]]; then
    printf "Config file: %s\n" "$GHOSTTY_CONFIG"
    if grep -Eq '^[[:space:]]*desktop-notifications[[:space:]]*=[[:space:]]*true[[:space:]]*$' "$GHOSTTY_CONFIG"; then
      pass "ghostty desktop-notifications = true"
    else
      fail "ghostty desktop-notifications = true"
      printf "${YELLOW}Tip:${NC} add this line to Ghostty config: desktop-notifications = true\n"
    fi
  else
    fail "ghostty config exists"
    printf "${YELLOW}Tip:${NC} create: %s\n" "$GHOSTTY_CONFIG"
  fi
}

test_desktop_notification_direct() {
  print_header "Desktop Notification (Direct)"
  run_expect_zero "osascript display notification" \
    osascript -e 'display notification "Notifs desktop direct test" with title "Notifs"'
}

test_terminal_escapes() {
  print_header "Terminal Escapes"

  local osc9_msg
  local osc777_msg
  osc9_msg="$(unique_message "Notifs OSC9")"
  osc777_msg="$(unique_message "Notifs OSC777")"

  info "Use this section twice: once focused, once after switching to another app"
  info "Waiting ${PRE_SEND_SLEEP_SECONDS}s so you can switch focus if needed..."
  sleep "$PRE_SEND_SLEEP_SECONDS"

  if [[ "${TERM_PROGRAM:-}" == "ghostty" || "${TERM:-}" == "xterm-ghostty" ]]; then
    info "Ghostty detected: sending Claude-compatible OSC 777 first"
    printf -- "- Sending OSC 777...\n"
    printf '\033]777;notify;Notifs;%s\007' "$osc777_msg"
    info "osc777 sequence sent"
    sleep 1
  fi

  printf -- "- Sending OSC 9...\n"
  printf '\033]9;%s\007' "$osc9_msg"
  info "osc9 sequence sent"
  sleep 1

  printf -- "- Sending OSC 777...\n"
  printf '\033]777;notify;Notifs;%s\007' "$osc777_msg"
  info "osc777 sequence sent"
  sleep 1

  printf -- "- Sending BEL...\n"
  printf '\a'
  info "bel sequence sent"

  manual_assert "Any terminal escape visible/audible" "Did you see or hear at least one terminal notification from OSC9/OSC777/BEL?"
}

manual_assert() {
  local name="$1"
  local question="$2"

  if [[ "$MANUAL_PROMPTS" -ne 1 ]]; then
    printf "${YELLOW}SKIP${NC} %s (manual prompts disabled)\n" "$name"
    return
  fi

  if [[ ! -t 0 ]]; then
    printf "${YELLOW}SKIP${NC} %s (non-interactive shell)\n" "$name"
    return
  fi

  local answer
  printf "%s [y/N]: " "$question"
  read -r answer
  case "$answer" in
    [Yy]|[Yy][Ee][Ss])
      pass "$name"
      ;;
    *)
      fail "$name"
      ;;
  esac
}

parse_args() {
  for arg in "$@"; do
    case "$arg" in
      --no-manual)
        MANUAL_PROMPTS=0
        ;;
      --manual)
        MANUAL_PROMPTS=1
        ;;
      --sleep-before=*)
        PRE_SEND_SLEEP_SECONDS="${arg#*=}"
        ;;
      --help|-h)
        printf "Usage: %s [--no-manual|--manual] [--sleep-before=SECONDS]\n" "$(basename "$0")"
        printf "  --no-manual            Skip interactive yes/no checks (default)\n"
        printf "  --manual               Enable interactive yes/no checks\n"
        printf "  --sleep-before=SECONDS Sleep before terminal sends (default: 5)\n"
        exit 0
        ;;
      *)
        printf "Unknown argument: %s\n" "$arg"
        printf "Use --help for usage.\n"
        exit 2
        ;;
    esac
  done
}

test_notifstestapp_modes() {
  print_header "NotifsTestApp Modes"

  run_cmd "DesktopOnly mode" \
    dotnet run --project "$APP_PROJECT" -- --mode desktop --verbose-routing --no-throw
  sleep 1

  run_cmd "TerminalOnly mode (auto)" \
    dotnet run --project "$APP_PROJECT" -- --mode terminal --terminal auto --verbose-routing --no-throw
  sleep 1

  run_cmd "TerminalOnly mode (osc9)" \
    dotnet run --project "$APP_PROJECT" -- --mode terminal --terminal osc9 --verbose-routing --no-throw
  sleep 1

  run_cmd "TerminalOnly mode (bel)" \
    dotnet run --project "$APP_PROJECT" -- --mode terminal --terminal bel --verbose-routing --no-throw
  sleep 1

  run_cmd "AutoDesktopFirst mode" \
    dotnet run --project "$APP_PROJECT" -- --mode auto --verbose-routing --no-throw
  sleep 1

  run_cmd "AutoTerminalFirst mode" \
    dotnet run --project "$APP_PROJECT" -- --mode autoterminal --verbose-routing --no-throw

  manual_assert "TerminalOnly mode is visible" "Did you see a terminal notification during TerminalOnly mode tests?"
}

print_summary() {
  print_header "Summary"
  printf "Passed: %s\n" "$PASSED"
  printf "Failed: %s\n" "$FAILED"

  if [[ "$FAILED" -gt 0 ]]; then
    printf "${RED}Some checks failed.${NC}\n"
    return 1
  fi

  printf "${GREEN}All command-based checks passed.${NC}\n"
  printf "${YELLOW}Note:${NC} terminal popups remain visually manual to verify.\n"
  return 0
}

main() {
  parse_args "$@"
  show_env
  check_ghostty_config
  test_desktop_notification_direct
  test_terminal_escapes
  test_notifstestapp_modes
  print_summary
}

main "$@"
