using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeyManagementWeb.Models
{
    public class User
    {
        [Key]
        [Column("userid")]
        public int UserId { get; set; }

        [Column("username")]
        public string? Username { get; set; }

        [Column("userpassword")]
        [StringLength(100)]
        public string? UserPassword { get; set; }

        [Column("usermail")]
        public string? UserMail { get; set; }

        [Column("user_permission")]
        public string? UserPermission { get; set; }
    }

    public class Key
    {

        [Key]
        [Column("keyid")]
        public int KeyId { get; set; }

        [Column("keytype")]
        public string? KeyType { get; set; }

        [Column("keyvalue")]
        public string? KeyValue { get; set; }

        [Column("insertdate")]
        public DateTime InsertDate { get; set; }

        [Column("keyupdateday")]
        public int KeyUpdateDay { get; set; }

        [Column("lastupdateday")]
        public DateTime LastUpdateDay { get; set; }
    }

    public class KeyHistory
    {
        [Key]
        [Column("historyid")]
        public int HistoryId { get; set; }

        [ForeignKey("Key")]
        [Column("keyid")]
        public int KeyId { get; set; }

        [Column("keyvalue")]
        public string? KeyValue { get; set; }

        [Column("changedate")]
        public DateTime ChangeDate { get; set; }

        [Column("changedby")]
        [Required]
        public string ChangedBy { get; set; } = "WinService";
    }
}
