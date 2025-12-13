# Changelog

all notable changes to GloomeClasses since the previous release (v1.0.10).

## [1.2.0] - 2025-12-13

#### New Diagnostics System
- **New Diagnostic Logging Framework** ([DiagnosticLogger.cs](src/Diagnostics/DiagnosticLogger.cs))
  - diagnostic logging with categorized messages based on class of origin
  - startup diagnostics showing mod version and enabled features
  - automatic detection of problematic third-party mods (RacialEquality, GloomeRaces, etc.)
  - compatibility testing with any other class-altering mods
    - character class detection and categorization (vanilla vs GloomeClasses vs other mods)
    - `.charsel` command logging for debugging character selection issues
  - player environment logging for trait debugging
  - session diagnostic summary reports

- **Mesh Generation Diagnostics** ([MeshDiagnostics.cs](src/Utils/MeshDiagnostics.cs))
  - mesh generation is tracked, caching statistics to diagnose GL buffer warnings
  - cache performance analysis with automatic warnings for poor performers
  - new `.meshdiag` command for generating diagnostic reports
  - might help identify excessive mesh regeneration issues

- **Trait System Diagnostics** ([TraitSystemDiagnostics.cs](src/Diagnostics/TraitSystemDiagnostics.cs))
  - runtime monitoring of trait application and effects to see what's working, what isn't

- **Character System Diagnostic Patches** ([CharacterSystemDiagnosticPatches.cs](src/Diagnostics/Patches/CharacterSystemDiagnosticPatches.cs))
  - automatic logging during character creation and trait assignment
  - reflection-based access to internal CharacterSystem for diagnostics

- **Agoraphobia Diagnostic Patches** ([AgoraphobiaDiagnosticsPatches.cs](src/Diagnostics/Patches/AgoraphobiaDiagnosticsPatches.cs))
  - detailed logging for agoraphobia trait behavior
  - environmental detection debugging (171 lines of diagnostic code)

#### Centralized Logging Utility
- **New Log Helper Class** ([Log.cs](src/Utils/Log.cs))
  - consistent, tagged logging across all GloomeClasses components
  - exception logging with automatic stack traces
  - follows Vintage Story Logger API best practices

#### Dev Experience Improvements
- **Build System Enhancements**
  - automatic detection of install if `$(VINTAGE_STORY)` env variable is not set
  - improved build configuration for development workflow

- **Developer Documentation**
  - comprehensive README.md with installation and build instructions

- **Project Structure Improvements**
  - total folder structure reorganization for better maintainability
  - fixed nesting folder issues
  - better caps consistency across files

### Bug Fixes

#### Critical Fixes
- **Null Safety Improvements**
  - added null safety checks in variables causing hard crashes/CTDs
  - pattern matching null checks in diagnostic systems

- **Alchemist Class Fixes**
  - fixed alchemist barrel mechanics
  - improved GUI distance checking for metal barrels
  - better recipe handling and validation

- **Chef Class Fixes**
  - refurbished crock no longer burns players

- **Recipe & Localization Fixes**
  - fixed trailing comma issues in JSON files
  - Corrected handbook usage references
  - fixed saltpeter hyperlink in its handbook for creation
  - updated localizations for Russian and Ukrainian (still WIP)
  - better JSON layout and formatting

### Code Quality & Refactoring

#### Modern C# Practices
- **Syntax Modernization**
  - updated to modern C# syntax patterns
  - collection expressions using `[]` initializers
  - pattern matching with `is not` operators
  - file-scoped namespaces
  - target-typed new expressions
  - string interpolation improvements

- **Total Refactoring**
  - refactored alchemist block system with better separation of concerns
  - refactored block entities with improved debugging support
  - codebase-wide refactoring for maintainability

#### Code Organization
- **File Structure Improvements**
  - Better organization of source files by feature/class
  - Consistent naming conventions
  - Improved namespace organization
  - Added XML documentation comments to diagnostic systems

### Content Updates

#### Recipe Changes
- **Oathkeeper Recipe Updates**
  - new recipe for temporal gems to replace the impossible one

- **Recipe Improvements**
  - broken/impossible recipes in general have been fixed

### Technical Improvements

#### Harmony Patches
- enhanced patch organization with new diagnostic category
- better error handling in patch application
- improved reflection-based patching for compatibility

#### Mod System Initialization
- diagnostic systems initialized during mod startup
- mod conflict detection
- character system validation after asset loading

#### Performance
- mesh caching diagnostics to identify potential performance issues

### Known Issues

- some vanilla class complaints in logs remain unresolved (diagnostic logging added to help track down)

### For Developers

#### New APIs Available
- `DiagnosticLogger` - centralized diagnostic logging system
- `Log` - standardized logging helper
- `MeshDiagnostics` - mesh generation tracking

#### New Commands
- `.meshdiag` - generate mesh diagnostics report (requires controlserver privilege)

### Migration Guide

no migration needed. simply replace v1.0.10 with v1.2.0.

### Acknowledgments

- **Gloomeglo**: Original mod author and architecture
- **JonR**: Co-author and previous development

---