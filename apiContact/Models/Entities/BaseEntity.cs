using MongoDB.Bson.Serialization.Attributes;

namespace apiContact.Models.Entities
{
    /// <summary>
    /// Shared base for every persisted entity.
    /// Provides a GUID string Id, lifecycle timestamps, and a consistent soft-delete contract.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// GUID-based primary key, stored as a plain string in both MongoDB and the
        /// in-memory fallback. Seeds may override this with their own stable identifiers.
        /// </summary>
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>UTC timestamp set once on creation; never mutated afterwards.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Set to UtcNow whenever any field on the entity is changed.</summary>
        public DateTime? UpdatedAt { get; set; }

        // ── Soft-delete contract ──────────────────────────────────────────────
        /// <summary>
        /// True if the entity has been soft-deleted.
        /// Repositories should filter out soft-deleted records by default.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>UTC timestamp of the soft-delete operation.</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>Id of the user who triggered the soft-delete.</summary>
        public string? DeletedBy { get; set; }

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>
        /// Marks the entity as soft-deleted.
        /// Caller is responsible for persisting the change.
        /// </summary>
        public void SoftDelete(string? deletedBy = null)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Restores a previously soft-deleted entity.</summary>
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
