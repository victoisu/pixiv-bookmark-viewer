using PixivBookmarkViewer.Data.Works;
using PixivBookmarkViewer.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Data.Pixiv
{
	public class PixivArchiver
    {
		private SqliteConnection _connection;
		public string _dbName;
		private string _dbDir;
		private PixivApiService _pixivApi;

		public string DbPath => Path.Combine(_dbDir, _dbName);

        public PixivArchiver(PixivApiService pixivApi)
		{
			_pixivApi = pixivApi;
		}

        public void Initialize(string dbDir)
		{
#if DEBUG
			_dbName = $"{DateTime.Now:yyyyMMddHHmm}-debug-pixiv.db";
#else
			_dbName = $"{DateTime.Now:yyyyMMddHHmm}-pixiv.db";
#endif
			_dbDir = dbDir;

			//Console.WriteLine($"Initializing pixiv archive {DbPath}");
			Directory.CreateDirectory(_dbDir);
			string connectionString = $"Data Source={DbPath};";
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
			var writeAhead = _connection.CreateCommand();
			writeAhead.CommandText =
@"
    PRAGMA journal_mode = 'wal';
";
			writeAhead.ExecuteNonQuery();
			CreateTables();
		}

		public async Task ArchiveAsync(CancellationToken token = default)
		{
			await GetAllBookmarks(token);
			//await GetAllPages(token);
			_connection.Close();
		}

		public static IEnumerable<BookmarkWork> GetArchivedWorks(SqliteConnection connection)
		{
			var command = connection.CreateCommand();
			command.CommandText = 
				@"
SELECT id, title, illust_type, user_id, user_name, bookmark_id, width, height, 
page_count, bookmarkable, create_date, update_date, unlisted
FROM works";

			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				int index = 0;
				yield return new BookmarkWork
				{
					Id = reader.GetInt32(index++),
					Title = reader.GetString(index++),
					IllustType = reader.GetInt32(index++),
					UserId = reader.GetInt32(index++),
					UserName = reader.GetString(index++),
					BookmarkId = reader.GetInt64(index++),
					Width = reader.GetInt32(index++),
					Height = reader.GetInt32(index++),
					PageCount = reader.GetInt32(index++),
					IsBookmarkable = reader.GetBoolean(index++),
					CreateDate = reader.GetDateTime(index++),
					UpdateDate = reader.GetDateTime(index++),
					IsUnlisted = reader.GetBoolean(index++),

					PersonalTags = Array.Empty<string>(),
					PublicTags = Array.Empty<string>()
				};
				
			}
			yield break;
		}

		public static IEnumerable<Page> GetArchivedPages(SqliteConnection otherConnection)
		{
			var command = otherConnection.CreateCommand();
			command.CommandText =
				@"
SELECT work_id, page_number, original_url, 
width, height, extension, filename 
FROM pages;
";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				int index = 0;
				yield return new Page
				{
					WorkId = reader.GetInt32(index++),
					PageNumber = reader.GetInt32(index++),
					OriginalUrl = reader.GetString(index++),
					Width = reader.GetInt32(index++),
					Height = reader.GetInt32(index++),
					Extension = reader.GetString(index++),
					FileName = reader.GetString(index++)
				};
			}
			yield break;
		}

		public static IEnumerable<(int, string, bool)> GetAllTags(SqliteConnection otherConnection)
		{
			var command = otherConnection.CreateCommand();
			command.CommandText = @"
SELECT w.id, t.name, t.public FROM tags_works tw
JOIN works w ON w.id = tw.work_id
JOIN tags t ON t.id = tw.tag_id;
";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				int index = 0;
				yield return (reader.GetInt32(index++), reader.GetString(index++), reader.GetBoolean(index++));
			}
			yield break;
		}

		private void CreateTables()
		{
			var command = _connection.CreateCommand();
			command.CommandText = SqlStatements.CreatePixivArchiveTables;
			command.ExecuteNonQuery();
		}

        private async Task GetAllBookmarks(CancellationToken token = default)
		{
			ConcurrentBag<BookmarkWork> works = new();
#if DEBUG
			await foreach (var work in await _pixivApi.GetBookmarks(5000))
#else
			await foreach (var work in await _pixivApi.GetBookmarks())
#endif
			{
				works.Add(work);
			}

			List<(int, string, bool)> workTags = new();
			HashSet<(string, bool)> tagSet = new();
			int n = 0;

			var transaction = _connection.BeginTransaction();
			var command = _connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"INSERT OR IGNORE INTO works VALUES (
$id,
$title,
$illustType,
$userId,
$userName,
$bookmarkId,
$width,
$height,
$pageCount,
$bookmarkable,
$createDate,
$updateDate,
$unlisted,
$thumbnail
);
";

			command.Parameters.Add("$id", SqliteType.Integer);
			command.Parameters.Add("$title", SqliteType.Text);
			command.Parameters.Add("$illustType", SqliteType.Integer);
			command.Parameters.Add("$userId", SqliteType.Integer);
			command.Parameters.Add("$userName", SqliteType.Text);
			command.Parameters.Add("$bookmarkId", SqliteType.Integer);
			command.Parameters.Add("$width", SqliteType.Integer);
			command.Parameters.Add("$height", SqliteType.Integer);
			command.Parameters.Add("$pageCount", SqliteType.Integer);
			command.Parameters.Add("$bookmarkable", SqliteType.Integer);
			command.Parameters.Add("$createDate", SqliteType.Integer);
			command.Parameters.Add("$updateDate", SqliteType.Integer);
			command.Parameters.Add("$unlisted", SqliteType.Integer);
			command.Parameters.Add("$thumbnail", SqliteType.Text);
			command.Parameters.Add("$original", SqliteType.Text);
			command.Parameters.Add("$bookmarkCount", SqliteType.Integer);
			command.Parameters.Add("$likeCount", SqliteType.Integer);
			command.Parameters.Add("$commentCount", SqliteType.Integer);
			command.Parameters.Add("$viewCount", SqliteType.Integer);

			foreach (var work in works)
			{
				if (token.IsCancellationRequested)
				{
					transaction.Commit();
					return;
				}
				command.Parameters["$id"].Value = work.Id;
				command.Parameters["$title"].Value = work.Title;
				command.Parameters["$illustType"].Value = work.IllustType;
				command.Parameters["$userId"].Value = work.UserId;
				command.Parameters["$userName"].Value = work.UserName;
				command.Parameters["$bookmarkId"].Value = work.BookmarkId;
				command.Parameters["$width"].Value = work.Width;
				command.Parameters["$height"].Value = work.Height;
				command.Parameters["$pageCount"].Value = work.PageCount;
				command.Parameters["$createDate"].Value = work.CreateDate;
				command.Parameters["$updateDate"].Value = work.UpdateDate;
				command.Parameters["$bookmarkable"].Value = work.IsBookmarkable;
				command.Parameters["$unlisted"].Value = work.IsUnlisted;

				command.Parameters["$thumbnail"].Value = "";//work.ThumbnailUrl;
				foreach (var tag in work.PersonalTags)
				{
					workTags.Add((work.Id, tag, false));
					tagSet.Add((tag, false));
				}

				foreach (var tag in work.PublicTags)
				{
					workTags.Add((work.Id, tag, true));
					tagSet.Add((tag, true));
				}

				command.ExecuteNonQuery();
				n++;
			}

			transaction.Commit();
			transaction = _connection.BeginTransaction();
			command = _connection.CreateCommand();
			command.Transaction = transaction;

			command.CommandText = @"
INSERT INTO tags (name, public) VALUES ($name, $public);
";
			command.Parameters.Add("$work_id", SqliteType.Integer);
			command.Parameters.Add("$name", SqliteType.Text);
			command.Parameters.Add("$public", SqliteType.Integer);

			foreach (var (tag, isPublic) in tagSet)
			{
				if (token.IsCancellationRequested)
				{
					transaction.Commit();
					return;
				}
				command.Parameters["$name"].Value = tag;
				command.Parameters["$public"].Value = isPublic;
				command.ExecuteNonQuery();
			}
			transaction.Commit();
			transaction = _connection.BeginTransaction();

			var ids = GetTagIds();

			command = _connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = @"
INSERT OR IGNORE INTO tags_works (work_id, tag_id)
VALUES ($work_id, $tag_id);
";
			command.Parameters.Add("$work_id", SqliteType.Integer);
			command.Parameters.Add("tag_id", SqliteType.Integer);
			foreach (var (work, tag, isPublic) in workTags)
			{
				if (token.IsCancellationRequested)
					break;
				command.Parameters["$work_id"].Value = work;
				command.Parameters["tag_id"].Value = ids[(tag, isPublic)];
				command.ExecuteNonQuery();
			}
			transaction.Commit();
		}

		private Dictionary<(string, bool), int> GetTagIds()
		{
			Dictionary<(string, bool), int> result = new();
			var command = _connection.CreateCommand();
			command.CommandText = @"SELECT id, name, public FROM tags";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				result.Add((reader.GetString(1), reader.GetBoolean(2)), reader.GetInt32(0));
			}
			return result;
		}
	}
}
