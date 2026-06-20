---
name: MongoDB double-sort bug
description: Chaining SortBy then SortByDescending (or vice versa) in the MongoDB C# driver replaces the first sort
---

## Rule
Never chain two `.SortBy()` / `.SortByDescending()` calls on the same fluent query. The second one silently replaces the first.

## Symptom
`GetByRoomAsync` had `.SortByDescending(m => m.Timestamp).Skip(skip).Limit(limit).SortBy(m => m.Timestamp)` — the intent was "page newest-first, then re-order asc for display". In MongoDB this produced only an ascending sort with wrong pagination.

## Fix
Fetch the page with the primary sort, then re-sort the result list in application memory:
```csharp
var page = await _col!.Find(filter)
    .SortByDescending(m => m.Timestamp)
    .Skip(skip).Limit(limit)
    .ToListAsync();
return page.OrderBy(m => m.Timestamp).ToList();
```
The in-memory (LINQ) path already did this correctly.

**Why:** The driver's fluent Sort API is stateful — the last Sort call wins. The in-memory LINQ path was always correct; the MongoDB path was silently broken.
