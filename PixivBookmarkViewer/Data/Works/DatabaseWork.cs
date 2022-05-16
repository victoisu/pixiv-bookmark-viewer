using PixivBookmarkViewer.Data.Pixiv;
using PixivBookmarkViewer.Data.Works.Interfaces;
using Microsoft.Data.Sqlite;

namespace PixivBookmarkViewer.Data
{
    public record DatabaseWork : MinWork, IWorkDatabase
	{

		#region Properties
		public int BookmarkCount { get; init; }

		public int LikeCount { get; init; }

		public int CommentCount { get; init; }

		public int ViewCount { get; init; }
		#endregion

		public static DatabaseWork FromReader(SqliteDataReader reader, int index = 0)
		{
			return new DatabaseWork
			{
				Id = reader.GetInt32(index++),
				Title = reader.IsDBNull(index++) ? null : reader.GetString(index-1),
				IllustType = reader.GetInt32(index++),
				UserId = reader.GetInt32(index++),
				UserName = reader.IsDBNull(index++) ? null : reader.GetString(index-1),
				BookmarkId = reader.GetInt64(index++),
				Width = reader.GetInt32(index++),
				Height = reader.GetInt32(index++),
				PageCount = reader.GetInt32(index++),
				IsBookmarkable = reader.GetBoolean(index++),
				CreateDate = reader.GetDateTime(index++),
				UpdateDate = reader.GetDateTime(index++),
				IsUnlisted = reader.GetBoolean(index++),
				BookmarkCount = reader.GetInt32(index++),
				LikeCount = reader.GetInt32(index++),
				CommentCount = reader.GetInt32(index++),
				ViewCount = reader.GetInt32(index++),
			};
		}

		public override IWorkDatabase ToDatabaseWork() => this;
    }
}
