# Validation — Command Palette 0.1.0

Validated on October 14, 2026. Production build: .NET SDK 10.0.112, Release,
zero warnings and errors. Core dependency source is pinned to
`beafd5738039dc3a6b641847f22bfd7ccfe2cd57` (0.1.4), separately from ongoing Core
and Better Shortcuts development. Release qualification also ran against merged
Core 0.1.5 (`23336c5`), with all profile folders redirected inside the disposable
Designer copy.

## Automated checks

| Layer | Result | Scope |
| --- | --- | --- |
| Portable logic | 21 passed | Exact/prefix/fuzzy search, case and multiple terms, source/alias/key search, unavailable and empty results, bounded recent history, stable ordering, one-shot dispatch, stale context, failed focus, changed availability, no retry after an exception. |
| Native Windows WPF harness | 16 passed | Search focus, keyboard/page navigation, unavailable and empty results, Enter and Escape, exact original text focus/selection, changed document/selection, disabled target, click-away cancellation. Designer discovery and theme are substituted in this harness. |
| Actual isolated Designer 10.5 | 17 passed | Real host guards, module load without Better Shortcuts, Core's Ctrl+Shift+P binding, live native command discovery, actual palette focus, disabled/empty results, Escape, exact original-editor restoration, single execution, recent ordering, native theme command routing, clean blank design. |
| Actual Designer with Core 0.1.5 | 18 passed | The same checks, plus synthetic Ctrl+Shift+P through Core's real preview handler and no duplicate direct binding. Core 0.1.5 intentionally routes mod keys through that guarded handler. |

The native probe uses the production module and normal runtime hash checks. The
initial palette contained 104 searchable commands; the test subsequently added
one synthetic action to verify dispatch and recent ordering. Core 0.1.5 exposed
107 searchable commands before that test action. That test action
is never packaged. The real native light/dark command was invoked through the
palette, and both theme renders were visually inspected for readable search,
results, selection, shortcuts, and status text.

The native test exposed that `Window.Activate()` can return false while Designer
has become active during its focus handlers. The final code checks the current
active window and keyboard-focus identity after the handlers run, keeping the
exact-target requirement. Earlier failed receipts are retained locally and do
not count as acceptance.

## Scope limits

- The native probe invoked the registered routed command and raised WPF key
  events. For Core 0.1.5, a test keyboard device supplies Ctrl/Shift modifier
  state to the actual preview route. This verifies shortcut dispatch; it is not a
  physical Ctrl+Shift+P or alternate-keyboard-layout test.
- Native acceptance covers an empty main-window design and a theme operation.
  Schematics with selected components, UCI editing, embedded script/browser
  focus, live Run/Emulate connections, and floating windows need separate checks.
- Core owns the shortcut engine, settings, remapping, and replacement routing.
  The palette displays its effective mapping and dispatches canonical native
  commands. Core 0.1.5 is qualified above; saved remaps, conflicts, Clear/Reset,
  and Better Packing together have not received palette-specific native tests.
- WPF tests cover stale document and selection rejection. Forced asynchronous
  document changes during an actual Designer palette session were not exercised.
- These runtime checks do not modify saved designs or establish DSP/hardware
  acceptance. Release/catalog and regular-install verification are separate steps.

## Package boundary

The schema-3 archive is checked by QMM's catalog-pinned packer against the
approval declarations. This verifies archive contents,
assembly identity/version, hashes, compatibility, and dependency declarations.
It does not approve that new identity, establish published author provenance,
or make the package available in the installed signed catalog.

The package contains only the palette DLL. Vendor references, Core runtime,
Harmony, the acceptance probe, debug symbols, and generated builds are excluded.
The independent Core dependency keeps its own exact-host guard and lifecycle.

Native receipts and rendered captures remain in the local development and
release workspaces. Reproduce the automated logic and WPF
checks using the README. Run the native probe only in a dedicated copied
Designer directory with `.selection-probe-lab` and `.palette-lab.json`:

```json
{
  "ApplicationDirectory": "<absolute isolated Designer directory>",
  "OutputDirectory": "<absolute new output directory>"
}
```

The probe refuses a mismatched application path or existing result, waits for
mod readiness, requires an untitled blank design, records its result, and exits
only that disposable process without saving Designer preferences.
Core 0.1.5's live settings interface additionally requires its test-only
`tests/LiveSettings/ProfileHook` and `.settings-live-lab.json` configuration to
redirect all Windows profile folders inside that copied application directory.
Never distribute or install either test helper in regular Designer.
