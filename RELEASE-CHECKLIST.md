# Public experimental distribution checklist

- [x] MIT license applied in repository and plugin source package.
- [x] Latest v0.4.2 portable ZIP includes MIT LICENSE, README, one executable and its checksum.
- [x] Public-facing documentation states unofficial status, unsigned binaries and Codex-only quota scope.
- [x] User authorized a separate clean public repository, without earlier history; the original repository must remain private and unchanged.
- [ ] Publication and anonymous download verification (checked by the publishing workflow, not implied by this source checklist).

## Validation limits — still open

- [ ] Real desktop-host plugin installation, sidebar/thread launch and UI refresh.
- [ ] Clean Windows PC installation, CLI login and network recovery.
- [ ] Latest v0.4.2 binary on a second physical computer.
- [ ] Windows ARM64, physical multi-monitor DPI changes and other OS/Node major versions.
- [ ] Signed Windows binaries and SmartScreen reputation.
- [ ] Independent security audit.

Historical tests and known plugin robustness findings are recorded in VALIDATION.md and windows-v0.4.2/VALIDATION.md. Public availability does not close these gaps. Do not advertise the plugin as a universally double-clickable Windows app. No automatic publishing workflow is included.
