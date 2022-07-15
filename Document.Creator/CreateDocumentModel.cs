namespace Document.Creator
{
    public class CreateDocumentModel
    {
        public int UserId { get; set; }
        public string Url { get; set; }
        public DocumentType DocumentType { get; set; }
    }

    public enum DocumentType
    {
        Pdf,
        Html,
        Png
    }
}