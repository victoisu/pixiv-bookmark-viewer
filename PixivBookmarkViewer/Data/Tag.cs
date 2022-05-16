namespace PixivBookmarkViewer.Data
{
    public record Tag : ITag
    {
        public string Name {get; init;}

        public bool IsPublic { get; init; }
    }
}
