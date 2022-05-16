namespace PixivBookmarkViewer.Data.Works.Interfaces
{
    public interface IWorkDatabase : IWork
    {
        public int BookmarkCount { get; }
        public int LikeCount { get; }
        public int CommentCount { get; }
        public int ViewCount { get; }
    }
}
