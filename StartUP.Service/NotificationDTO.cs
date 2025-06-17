namespace StartUP.Service
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string SenderName { get; set; }
        public string SenderPhoto { get; set; }
        public string ProjectName { get; set; }
        public int ProjectId { get; set; }
        public string Message { get; set; }
        public string FullMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public bool IsUnread { get; set; }
    }
}