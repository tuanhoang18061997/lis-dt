using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models.Pacs
{
    [Table("PacsSyncStates")]
    public class PacsSyncState
    {
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// PACS Server tương ứng trong bảng PacsConfigs.
        /// Mỗi PacsConfigId chỉ có một dòng trạng thái sync.
        /// </summary>
        public long PacsConfigId { get; set; }

        /// <summary>
        /// Sequence cuối cùng đã xử lý thành công từ Orthanc /changes.
        /// Lần kế tiếp sẽ gọi /changes?since=LastChangeSeq.
        /// </summary>
        public long LastChangeSeq { get; set; } = 0;

        /// <summary>
        /// Thời điểm bắt đầu lượt chạy gần nhất.
        /// </summary>
        public DateTime? LastRunAt { get; set; }

        /// <summary>
        /// Thời điểm gần nhất hoàn tất sync thành công.
        /// </summary>
        public DateTime? LastSuccessAt { get; set; }

        /// <summary>
        /// Nội dung lỗi gần nhất.
        /// </summary>
        public string? LastError { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}