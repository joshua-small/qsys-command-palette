# Validation — Command Palette 0.1.0

Validated on October 14, 2026. Production build: .NET SDK 10.0.112, Release,
zero warnings and errors. Core dependency source is pinned to
`beafd5738039dc3a6b641847f22bfd7ccfe2cd57` (0.1.4), separately from ongoing Core
and Better Shortcuts development.

## Automated checks

| Layer | Result | Scope |
| --- | --- | --- |
| Portable logic | 21 passed | Exact/prefix/fuzzy search, case and multiple terms, source/alias/key search, unavailable and empty results, bounded recent history, stable ordering, one-shot dispatch, stale context, failed focus, changed availability, no retry after an exception. |
| Native Windows WPF harness | 16 passed | Search focus, keyboard/page navigation, unavailable and empty results, Enter and Escape, exact original text focus/selection, changed document/selection, disabled target, click-away cancellation. Designer discovery and theme are substituted in this harness. |
| Actual isolated Designer 10.5 | 17 passed | Real host guards, module load without Better Shortcuts, Core's Ctrl+Shift+P binding, live native command discovery, actual palette focus, disabled/empty results, Escape, exact original-editor restoration, single execution, recent ordering, native theme command routing, clean blank design. |

The native probe uses the production module and normal runtime hash checks. The
initial palette contained 104 searchable commands; the test subsequently added
one synthetic action to verify dispatch and recent ordering. That test action
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
  events. This verifies input-binding registration and dispatch; it is not a
  physical Ctrl+Shift+P or alternate-keyboard-layout test.
- Native acceptance covers an empty main-window design and a theme operation.
  Schematics with selected components, UCI editing, embedded script/browser
  focus, live Run/Emulate connections, and floating windows need separate checks.
- Core owns the shortcut engine, settings, remapping, and replacement routing.
  The palette displays its effective mapping and dispatches canonical native
  commands. Saved remaps, conflicts, Clear/Reset, newer chord-enabled Core builds,
  and Better Packing together have not received palette-specific native tests.
- WPF tests cover stale document and selection rejection. Forced asynchronous
  document changes during an actual Designer palette session were not exercised.
- No Core deployment, saved design modification, regular-install update,
  stable release, or signed-catalog acceptance is claimed.

## Package boundary

The candidate schema-3 archive is checked by QMM's packer against a local copy
of the approvals plus the proposed identity. This verifies archive contents,
assembly identity/version, hashes, compatibility, and dependency declarations.
It does not approve that new identity, establish published author provenance,
or make the package available in the installed signed catalog.

The package contains only the palette DLL. Vendor references, Core runtime,
Harmony, the acceptance probe, debug symbols, and generated builds are excluded.
The independent Core dependency keeps its own exact-host guard and lifecycle.

Native receipts and rendered captures remain in the local development
workspace's `verification/` directory. Reproduce the automated logic and WPF
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
