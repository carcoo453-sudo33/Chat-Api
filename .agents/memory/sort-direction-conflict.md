---
name: SortDirection naming conflict
description: CS0104 ambiguity between custom enum and MongoDB.Driver.SortDirection
---

# SortDirection Naming Conflict

## Rule
Never name a custom enum `SortDirection` in this project.

## Why
`MongoDB.Driver` exports its own `SortDirection` enum. Any file that imports both `apiContact.Models.Enums` and `MongoDB.Driver` namespaces (e.g. `RoomRepository.cs`) will hit CS0104 ambiguous reference error.

## Fix applied
Renamed `apiContact.Models.Enums.SortDirection` → `SortOrder`. Updated all references in:
- `Models/Enums/SortDirection.cs` (file kept, enum renamed)
- `Models/Dtos/PaginationDtos.cs`
- `Data/Repositories/RoomRepository.cs`

**How to apply:** Any future sort-direction enum must use `SortOrder`, `QuerySortOrder`, or another name that doesn't clash with MongoDB.Driver.
