---
name: Soft-delete pattern
description: How soft-delete works across all entities; filter rules for repositories
---

## Rule
All entities extend `BaseEntity` which provides `SoftDelete()`, `Restore()`, `IsDeleted`, `DeletedAt`, `DeletedBy`, `UpdatedAt`.

## Delete behaviour
- **Message** — soft-deleted; `Content` replaced with `"[Message deleted]"` so conversation flow is intact. Was already soft-deleted; updated to use `msg.SoftDelete()` helper.
- **User** — soft-deleted (was previously a hard `_db.Users.Remove(id)` / `DeleteOneAsync`). Preserves authorship references on messages.
- **Room** — soft-deleted (was previously a hard delete). Messages inside the room remain intact.

## Repository filter rules
Every read operation (GetByIdAsync, GetAllAsync, FindAsync, GetOnlineUsersAsync, SearchAsync, GetByUserAsync, GetBySlugAsync, SlugExistsAsync, GetAllCategoriesAsync, GetAllTagsAsync) must apply `!entity.IsDeleted`. The `!IsDeleted` condition is the first element in all MongoDB filter lists.

**Why:** Without the filter, soft-deleted records still appear in listings, search results, and auth lookups (e.g. a deleted user could still log in if their token hadn't expired).

## Password on CreateUser
`CreateUserHandler` now calls `BC.HashPassword(dto.Password)` and passes the hash to `UserMapper.FromCreateDto`. Previously the raw password was stored as the hash — a silent security bug.
