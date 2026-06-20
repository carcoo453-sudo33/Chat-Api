---
name: Mappings layer
description: How entity↔DTO conversion is organised in the Chat API; where to add new mappings
---

## Rule
All mapping lives in `apiContact/Mappings/`. No anonymous objects in controllers or handlers.

## Files
- `UserMapper.cs` — `ToProfile()` → `UserProfileDto`, `ToPublicProfile()` → `UserPublicProfileDto`, `FromCreateDto()`, `FromRegisterDto()`, `ApplyUpdate()`
- `RoomMapper.cs` — `FromCreateDto(dto, callerId, uniqueSlug)`, `ApplyUpdate(room, dto, newSlug?)`
- `MessageMapper.cs` — `FromSendDto(dto, senderName)`

## Response records
`UserProfileDto` and `UserPublicProfileDto` are sealed records defined in `UserMapper.cs`. No separate DTO file needed.

## How to apply
- Handler creates entity: call `XxxMapper.FromCreateDto(dto, ...)` — Id/CreatedAt come from BaseEntity defaults (GUID/UtcNow), never set manually.
- Handler patches entity: call `XxxMapper.ApplyUpdate(entity, dto)` — always sets `UpdatedAt`.
- Controller projects list: `list.Select(UserMapper.ToProfile)`.

**Why:** Eliminated duplicate inline projections in UsersController (MapProfile/MapPublicProfile), RoomsController (GetMemberProfiles), and all three command handlers. Single source of truth for shape.
