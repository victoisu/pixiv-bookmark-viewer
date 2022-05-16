using System;

namespace PixivBookmarkViewer.Data.Works.Interfaces
{
    public interface IWork
    {
        public int Id { get; }

        public string Title { get; }

        public int IllustType { get; }

        public int UserId { get; }

        public string UserName { get; }
        public long BookmarkId { get; }

        public int Width { get; }

        public int Height { get; }

        public int PageCount { get; }

        public bool IsBookmarkable { get; }

        public bool IsUnlisted { get; }

        public DateTime CreateDate { get; }

        public DateTime UpdateDate { get; }

        public IWorkDatabase ToDatabaseWork();
    }
}
