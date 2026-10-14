# Command Palette

A VS Code-style command palette for Q-SYS Designer. Press **Ctrl+Shift+P** or
choose **Tools > Q-SYS Mods > Command Palette…**.

Type a command name, category, source, or shortcut. Search is case insensitive
and accepts abbreviations and multiple words. Use **Up/Down** or **Page Up/Page
Down** to select a result, **Enter** or double-click to run it, and **Escape**
to cancel. Clicking away closes the palette. Recently executed commands appear
first when the search is empty; history lasts for the current Designer session.

The palette shows native Designer commands and commands registered by enabled
mods, with the effective shortcut supplied by Q-SYS Mods Core. It needs **Core
0.1.4**, and does not require Better Shortcuts. If Better Shortcuts is installed,
the palette's shortcut can be remapped, cleared, or reset there like any other
registered mod command. Opening the palette does not write shortcut settings.

Unavailable commands remain searchable and show their registered context. When
Designer only supplies a yes/no availability result, the palette says that the
command is unavailable; it does not invent a more specific reason.

## Execution context

Availability is captured before the palette takes keyboard focus. Execution
closes the palette, restores that exact editor focus, and checks the provider,
document, active pages/canvas, selection, and command availability again. A
changed or detached context cancels execution. Native actions use the existing
routed command, including active replacements such as Better Packing.

This first version opens from the **main Designer window**. The separate floating
Component Controls window and embedded browser/VS Code keyboard interception
are not supported entry points. The palette exposes the Core command registry;
arbitrary reflection methods and click-only menu handlers are not commands.
The native dialogs, confirmation prompts, and undo behavior of an action remain
that action's responsibility.

## Candidate status

Version **0.1.0** is a local development candidate for the inspected Designer
**10.5.0.0** build. Core checks the exact Designer and ModuleCommon hashes before
initialization. No vendor DLL is modified or included in the package.

This repository has not been published, and the new mod identity has not been
approved in the signed QMM catalog. `catalog/approval-proposal.json` is a proposal
for onboarding, not an installed approval. The locally verified `.qdsmod` is
not yet importable through the normal signed catalog. No change has been made
to the regular Designer installation.

Proposed repository: `joshua-small/qsys-command-palette`. The help URL in the
presentation metadata is reserved for that future repository. Existing planning:
[Better Shortcuts #4](https://github.com/joshua-small/qsys-better-shortcuts/issues/4)
and [Mods Core #4](https://github.com/joshua-small/qsys-mods-core/issues/4).

## Build and verification

Place the pinned Core source alongside this repository as `../qsys-mods-core`.
The initial dependency is commit `beafd5738039dc3a6b641847f22bfd7ccfe2cd57`.
Use SDK 10.0.112 and external local Designer references:

```text
dotnet build -c Release --artifacts-path ../build/release -p:DesignerDir="<Designer 10.5 directory>"
dotnet run --project tests/Logic/Logic.csproj --artifacts-path ../build/logic
dotnet build tests/Wpf/Wpf.csproj -c Release --artifacts-path ../build/wpf
```

On WSL, use the mounted Windows Designer path. Execute the built WPF test DLL
with the native Windows .NET runtime. Keep all build output outside the source.

The native probe under `tests/Native` is an isolated test-only module. It requires
real matching binaries, a lab marker, an exact application-directory allowlist,
and a blank design. It is never shipped in the palette package. See
[validation](docs/VALIDATION.md) for results and untested scope.

## Packaging

The payload contains only `Designer.Modules.CommandPalette.dll`. The schema-3
layout declares `mods-core >= 0.1.4, < 0.2.0` and conflicts with the legacy
Schematic Tools bundle before 0.17. The consumer does not ship another Core
runtime or Harmony copy. The proposed layout follows the
[QMM package contract](https://github.com/joshua-small/qsys-mod-catalog/blob/main/PACKAGE-CONTRACT.md).

Before distribution, onboard the identity/layout, create the source repository,
pin the reviewed publisher workflow, and validate source/artwork provenance at
the release commit. Local packaging verification does not approve publication
or installation.
