# NOVA PC BOOST — QA checklist

This checklist is for Windows 10 and Windows 11 testing. A successful CI build is not a substitute for running these checks on real systems.

## Before testing
- Use a disposable Windows VM or a test PC where changes can be reverted.
- Create a Windows restore point or VM snapshot.
- Record the current power plan and service startup/running states.
- Test with a standard user and with UAC elevation where requested.

## Build and packaging
- [ ] GitHub Actions build completes successfully.
- [ ] Downloaded EXE starts on Windows 10 x64 and Windows 11 x64.
- [ ] Application starts without administrator rights.
- [ ] Service changes request UAC only when needed.
- [ ] Cancelling UAC leaves the original app usable and changes nothing.

## Registry tweaks
- [ ] Apply one tweak; verify the intended value changed.
- [ ] Restart NOVA; verify the backup survives restart.
- [ ] Restore one tweak; verify original values and missing-value state are restored.
- [ ] Force a write failure mid-operation; verify automatic rollback or retained backup.
- [ ] Corrupt `restore_state.json`; verify new tweaks/services are blocked and the file is not overwritten.
- [ ] Use “restore all” with registry-only backups; verify no remaining backups and inspect the action log.

## Windows services
- [ ] Test every listed service separately; verify exact startup type and whether it was running.
- [ ] Confirm stopping a service does not get reported as success when configuration did not change.
- [ ] Restore each service; verify startup type and actual running/stopped state.
- [ ] Test services that are absent, protected, or controlled by Windows.
- [ ] Test service restore from a standard account and with UAC cancellation.

## Diagnostics and FPS history
- [ ] Compare Windows/CPU/RAM/disk readings with Windows system tools.
- [ ] Verify unknown GPU data is shown as unavailable rather than fabricated.
- [ ] Reject blank, negative, non-finite, or 1% low values above average FPS.
- [ ] Save a valid measurement with game/scenario metadata and verify the JSON record.
- [ ] Corrupt `benchmark_results.json`; verify the previous file is not overwritten.
- [ ] Repeat a game benchmark in the same scene/settings and record the variance.

## Cleanup
- [ ] Review the confirmation text before deleting.
- [ ] Verify files newer than 48 hours and reparse points are skipped.
- [ ] Verify busy files and access-denied folders are skipped and counted.
- [ ] Confirm only temporary directories and the recycle bin are targeted.

## Update checker and site
- [ ] Check latest release with internet access.
- [ ] Check behavior offline and with GitHub rate limiting.
- [ ] Verify the checker only opens the official GitHub release and never downloads/runs an EXE itself.
- [ ] Verify the website demo clearly states that it does not change Windows or measure actual FPS.
- [ ] Verify website download points to the latest published release asset.

## Release gate
Do not label the app “10/10” or claim production-ready until all applicable checks above pass on actual Windows 10/11 systems. Record OS build, test date, result, and failures for each test.
