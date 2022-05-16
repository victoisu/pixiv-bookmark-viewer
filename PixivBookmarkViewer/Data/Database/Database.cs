using PixivBookmarkViewer.Data;
using PixivBookmarkViewer.Data.Pixiv;
using PixivBookmarkViewer.Data.Works;
using PixivBookmarkViewer.Data.Works.Interfaces;
using PixivBookmarkViewer.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PixivBookmarkViewer
{
    public class Database
	{

		private SqliteConnection _connection;
		private string DbPath { get; init; }

		public Database(IWebHostEnvironment env, string dbName)
		{
			DbPath = Path.Combine(env.ContentRootPath, $"{dbName}.db");
		}

		public Database()
		{
			DbPath = ":memory:";
		}

		private void OpenDatabase()
		{
			string connectionString = $"Data Source={DbPath};Mode=ReadWriteCreate;";
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}

		private void CloseDatabase()
		{
			_connection?.Close();
		}

		public void Initialize()
		{
			OpenDatabase();
			CreateTables();
			CreateCommands();
			GetMaxTagId();
		}

		private void CreateCommands()
		{
			GenerateUpsertWorkCommand();
			GenerateUpsertPageCommand();
			GenerateInsertTagCommand();
			GenerateInsertTagWorkCommand();
		}

		private void CreateTables()
		{
			CreateWorkTable();
			CreatePageTable();
			CreateTagTable();
			CreateIndices();
		}

		public void MergePixivArchive(string dbPath)
		{
			string connectionString = $"Data Source={dbPath};Mode=ReadOnly;";
			var otherConnection = new SqliteConnection(connectionString);
			otherConnection.Open();
			MergePixivArchiveWorks(otherConnection);

			otherConnection.Close();
		}

		public void InsertWorks(IEnumerable<BookmarkWork> works)
		{
			using var transaction = _connection.BeginTransaction();
			HashSet<(string, bool)> allTags = new();
			HashSet<(int, string, bool)> workTags = new();

			foreach (var work in works)
			{
				InsertWorkIntern(work.ToDatabaseWork(), transaction);
				foreach (var tag in work.PublicTags)
				{
					allTags.Add((tag, true));
					workTags.Add((work.Id, tag, true));
				}

				foreach (var tag in work.PersonalTags)
				{
					allTags.Add((tag, false));
					workTags.Add((work.Id, tag, false));
				}
			}

			int currentId = GetMaxTagId() + 1;
			Dictionary<(string, bool), int> tagIds = new();
			foreach (var (tag, isPublic) in allTags)
			{
				SetInsertTag(currentId, tag, isPublic);
				_insertTagCommand.ExecuteNonQuery();
				tagIds.Add((tag, isPublic), currentId);
				currentId++;
			}

			foreach (var (id, tag, isPublic) in workTags)
			{
				SetInsertTagWork(id, tagIds[(tag, isPublic)]);
				_tagWorkCommand.ExecuteNonQuery();
			}

			transaction.Commit();
		}

		public void InsertWork(BookmarkWork work)
		{
			InsertWorks(new[] { work });
		}

		public void InsertWorks(IEnumerable<IWorkDatabase> works)
        {
			using var transaction = _connection.BeginTransaction();
            foreach (var work in works)
            {
				InsertWorkIntern(work.ToDatabaseWork(), transaction);
			}
			transaction.Commit();
		}

		public void InsertWork(IWorkDatabase work)
        {
			InsertWorks(new[] { work });
        }

		private void InsertWorkIntern(IWorkDatabase work, SqliteTransaction transaction = null)
		{
			SetUpsertWork(work);
			_upsertWorkCommand.Transaction = transaction ?? _upsertWorkCommand.Transaction;
            _upsertWorkCommand.ExecuteNonQuery();
        }

		public void InsertPages(IEnumerable<Page> pages)
		{
			using var transaction = _connection.BeginTransaction();
			_upsertPageCommand.Transaction = transaction;

			foreach (var page in pages)
			{
				InsertPageIntern(page, transaction);
			}

			transaction.Commit();
		}

		public void InsertPage(Page page)
		{
			InsertPageIntern(page);
		}

		public Page GetPage(int workId, int pageNumber = 0)
		{
			var command = _connection.CreateCommand();
			command.CommandText = $"SELECT work_id, page_number, original_url, width, height, extension, filename FROM pages WHERE work_id = {workId} AND page_number = {pageNumber}";
			var reader = command.ExecuteReader();
			if (reader.Read())
			{
				int index = 0;
				return new Page
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
			else
			{
				return null;
			}
		}

		public IEnumerable<Page> GetPages()
		{
			var command = _connection.CreateCommand();
			command.CommandText = $"SELECT work_id, page_number, original_url, width, height, extension, filename";
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

		public IEnumerable<DatabaseTag> GetTags()
		{
			var command = _connection.CreateCommand();
			command.CommandText = @"SELECT * FROM tags";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				yield return DatabaseTag.FromReader(reader);
			}
			yield break;
		}

		public IEnumerable<(int, DatabaseTag)> GetTagsWorks()
		{
			var command = _connection.CreateCommand();
			command.CommandText = @"
SELECT w.id, t.id, t.name, t.public FROM tags_works tw
JOIN works w ON w.id = tw.work_id
JOIN tags t ON t.id = tw.tag_id;
";
			var reader = command.ExecuteReader();
			while (reader.Read())
			{
				int index = 0;
				yield return (reader.GetInt32(index++), DatabaseTag.FromReader(reader, index));
			}
			yield break;
		}

		public IEnumerable<FullWork> GetWorks()
		{
			var command = _connection.CreateCommand();
			command.CommandText = GetWorksStatement;
			var reader = command.ExecuteReader();
			DatabaseWork dbWork = null;
			List<string> personalTags = new();
			List<string> publicTags = new();
			while (reader.Read())
			{
				if (reader.GetInt32(0) != (dbWork?.Id))
				{
					if (dbWork != null)
						yield return FullWork.FromDatabase(dbWork, publicTags, personalTags);
					dbWork = DatabaseWork.FromReader(reader);
					personalTags = new();
					publicTags = new();
				}
				var tagName = reader.GetString(17);
				var isPublic = reader.GetBoolean(18);
				(isPublic ? publicTags : personalTags).Add(tagName);
			}
			yield break;
		}

		public IEnumerable<int> GetPagelessWorks()
        {
			var command = _connection.CreateCommand();
			command.CommandText = 
				@"
SELECT id FROM works 
LEFT JOIN pages ON pages.work_id = works.id 
WHERE page_number is NULL
ORDER BY bookmark_id DESC
";
			var reader = command.ExecuteReader();
            while (reader.Read())
            {
				yield return reader.GetInt32(0);
            }
			yield break;
        }

		public FullWork GetWork(int id)
        {
			var command = _connection.CreateCommand();
			command.CommandText = GetWorkStatement(id);
			var reader = command.ExecuteReader();

			List<string> personalTags = new();
			List<string> publicTags = new();
			DatabaseWork dbWork = null;
			while (reader.Read())
            {
				if (dbWork == null)
					dbWork = DatabaseWork.FromReader(reader);

				var tagName = reader.GetString(17);
				var isPublic = reader.GetBoolean(18);
				(isPublic ? publicTags : personalTags).Add(tagName);
			}
			return dbWork == null ? null : FullWork.FromDatabase(dbWork, publicTags, personalTags);
		}

		public WorkCollection GetWorkCollection()
		{
			return GetWorks().ToList().ToWorkCollection();
		}

		private void InsertPageIntern(Page page, SqliteTransaction transaction = null)
		{
			SetUpsertPage(page);
			_upsertPageCommand.Transaction = transaction ?? _upsertPageCommand.Transaction;
			_upsertPageCommand.ExecuteNonQuery();
		}

		private void MergePixivArchiveWorks(SqliteConnection otherConnection)
		{
			InsertWorks(PixivArchiver.GetArchivedWorks(otherConnection));
			InsertPages(PixivArchiver.GetArchivedPages(otherConnection));
			MergePixivArchiveTags(PixivArchiver.GetAllTags(otherConnection));
		}

		private void MergePixivArchiveTags(IEnumerable<(int, string, bool)> tags)
		{
			HashSet<(string, bool)> seen = new();
			Dictionary<(string, bool), int> tagToId = new();
			int currentId = GetMaxTagId() + 1;

			foreach (var tag in GetTags())
			{
				tagToId.Add((tag.Name, tag.IsPublic), tag.Id);
				seen.Add((tag.Name, tag.IsPublic));
			}

			using var transaction = _connection.BeginTransaction();
			_insertTagCommand.Transaction = transaction;
			_tagWorkCommand.Transaction = transaction;
			foreach (var (work, name, isPublic) in tags)
			{
				var tuple = (name, isPublic);
				if (!seen.Contains(tuple))
				{
					SetInsertTag(currentId, name, isPublic);
					_insertTagCommand.ExecuteNonQuery();
					tagToId.Add(tuple, currentId);
					seen.Add(tuple);
					currentId++;
				}

				SetInsertTagWork(work, tagToId[tuple]);
				_tagWorkCommand.ExecuteNonQuery();
			}
			transaction.Commit();
		}

		private int GetMaxTagId()
		{
			var command = _connection.CreateCommand();
			command.CommandText = @"SELECT max(id) FROM tags;";
			var reader = command.ExecuteReader();
			if (reader.Read())
			{
				
				return (reader.IsDBNull(0)) ? -1 : reader.GetInt32(0);
			}
			else
			{
				return -1;
			}
		}

		#region SQL statements
		#region Tables
		private void CreateWorkTable()
		{
			var command = _connection.CreateCommand();
			// Adds Data.Work equivalent table.
			// Missing direct mappings for:
			// All tags, UserName, IsPublicBookmark, BookmarkId
			command.CommandText = @"
CREATE TABLE IF NOT EXISTS works (
	id INTEGER PRIMARY KEY,
	title TEXT,
	illust_type INTEGER,
	user_id INTEGER,
	user_name TEXT,
	bookmark_id INTEGER DEFAULT -1,
	width INTEGER,
	height INTEGER,
	page_count INTEGER,
	bookmarkable BOOLEAN,
	create_date DATETIME,
	update_date DATETIME,
	unlisted BOOLEAN,
	bookmark_count INTEGER DEFAULT -1,
	like_count INTEGER DEFAULT -1,
	comment_count INTEGER DEFAULT -1,
	view_count INTEGER DEFAULT -1
);
";
			command.ExecuteNonQuery();
		}

		private void CreatePageTable()
		{
			var command = _connection.CreateCommand();
			// Adds Data.Page equivalent table.
			// Missing direct mappings for:
			// UserName, IsPublicBookmark, BookmarkId
			command.CommandText = @"
CREATE TABLE IF NOT EXISTS pages (
	work_id INTEGER,
	page_number INTEGER,
	original_url TEXT,
	width INTEGER,
	height INTEGER,
	extension TEXT,
	filename TEXT,
	PRIMARY KEY (work_id, page_number)/*,
	
	Work may not exist in database at time of insertion, therefore this constraint does not work.
	TODO: Decide whether to leave this (can get work info later, maybe backlogged) or enforce a check on the work.
	FOREIGN KEY (work_id) REFERENCES works(id)
	*/
);
";
			command.ExecuteNonQuery();
		}

		private void CreateTagTable()
		{
			var command = _connection.CreateCommand();
			command.CommandText = @"
CREATE TABLE IF NOT EXISTS tags (
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	name TEXT,
	public BOOLEAN
);

CREATE TABLE IF NOT EXISTS tags_works (
	work_id INTEGER NOT NULL,
	tag_id INTEGER NOT NULL,
	PRIMARY KEY(work_id, tag_id),
	FOREIGN KEY (work_id) REFERENCES works(id),
	FOREIGN KEY (tag_id) REFERENCES tags(id)
);
";
			command.ExecuteNonQuery();
		}

		private void CreateIndices()
		{
			var command = _connection.CreateCommand();
			command.CommandText = @"
CREATE INDEX IF NOT EXISTS idx_artist
ON works (user_id);
CREATE INDEX IF NOT EXISTS idx_illust_type
ON works (illust_type);
CREATE INDEX IF NOT EXISTS idx_tag
ON tags (name);
CREATE UNIQUE INDEX IF NOT EXISTS idx_fulltag
ON tags (name, public);
CREATE UNIQUE INDEX IF NOT EXISTS idx_workpage	
ON pages (work_id, page_number);
";
			command.ExecuteNonQuery();
		}
		#endregion

		#region Upserting Works
		private SqliteCommand _upsertWorkCommand;
		private string UpsertWork => @"
INSERT OR REPLACE INTO works 
 (id,  title,  illust_type,  user_id,  user_name,  bookmark_id,  width,  height,  page_count,  bookmarkable,  create_date,  update_date,  unlisted,  bookmark_count,  like_count,  comment_count,  view_count) 
VALUES 
($id, $title, $illust_type, $user_id, $user_name, $bookmark_id, $width, $height, $page_count, $bookmarkable, $create_date, $update_date, $unlisted, $bookmark_count, $like_count, $comment_count, $view_count)
";
		private void GenerateUpsertWorkCommand()
		{
			var command = _connection.CreateCommand();
			command.CommandText = UpsertWork;
			command.Parameters.Add("$id", SqliteType.Integer);
			command.Parameters.Add("$title", SqliteType.Text);
			command.Parameters.Add("$illust_type", SqliteType.Integer);
			command.Parameters.Add("$user_id", SqliteType.Integer);
			command.Parameters.Add("$user_name", SqliteType.Text);
			command.Parameters.Add("$bookmark_id", SqliteType.Integer);
			command.Parameters.Add("$width", SqliteType.Integer);
			command.Parameters.Add("$height", SqliteType.Integer);
			command.Parameters.Add("$page_count", SqliteType.Integer);
			command.Parameters.Add("$bookmarkable", SqliteType.Integer);
			command.Parameters.Add("$create_date", SqliteType.Integer);
			command.Parameters.Add("$update_date", SqliteType.Integer);
			command.Parameters.Add("$unlisted", SqliteType.Integer);
			command.Parameters.Add("$bookmark_count", SqliteType.Integer);
			command.Parameters.Add("$like_count", SqliteType.Integer);
			command.Parameters.Add("$comment_count", SqliteType.Integer);
			command.Parameters.Add("$view_count", SqliteType.Integer);
			_upsertWorkCommand = command;
		}
		private void SetUpsertWork(IWorkDatabase work)
		{
			_upsertWorkCommand.Parameters["$id"].Value = work.Id;
			_upsertWorkCommand.Parameters["$title"].Value = work.Title;
			_upsertWorkCommand.Parameters["$illust_type"].Value = work.IllustType;
			_upsertWorkCommand.Parameters["$user_id"].Value = work.UserId;
			_upsertWorkCommand.Parameters["$user_name"].Value = work.UserName;
			_upsertWorkCommand.Parameters["$width"].Value = work.Width;
			_upsertWorkCommand.Parameters["$bookmark_id"].Value = work.BookmarkId;
			_upsertWorkCommand.Parameters["$height"].Value = work.Height;
			_upsertWorkCommand.Parameters["$page_count"].Value = work.PageCount;
			_upsertWorkCommand.Parameters["$bookmarkable"].Value = work.IsBookmarkable;
			_upsertWorkCommand.Parameters["$create_date"].Value = work.CreateDate;
			_upsertWorkCommand.Parameters["$update_date"].Value = work.UpdateDate;
			_upsertWorkCommand.Parameters["$unlisted"].Value = work.IsUnlisted;
			_upsertWorkCommand.Parameters["$bookmark_count"].Value = work.BookmarkCount;
			_upsertWorkCommand.Parameters["$like_count"].Value = work.LikeCount;
			_upsertWorkCommand.Parameters["$comment_count"].Value = work.CommentCount;
			_upsertWorkCommand.Parameters["$view_count"].Value = work.ViewCount;
		}
		#endregion

		#region Upserting Pages
		private SqliteCommand _upsertPageCommand;
		private string UpsertPage => @"
INSERT OR IGNORE INTO pages (work_id, page_number, original_url, width, height, extension, filename)
VALUES ($work_id, $page_number, $original_url, $width, $height, $extension, $filename);
";
		private void GenerateUpsertPageCommand()
		{
			var command = _connection.CreateCommand();
			command.CommandText = UpsertPage;
			command.Parameters.Add("$work_id", SqliteType.Integer);
			command.Parameters.Add("$page_number", SqliteType.Integer);
			command.Parameters.Add("$original_url", SqliteType.Text);
			command.Parameters.Add("$width", SqliteType.Integer);
			command.Parameters.Add("$height", SqliteType.Integer);
			command.Parameters.Add("$extension", SqliteType.Text);
			command.Parameters.Add("$filename", SqliteType.Text);
			_upsertPageCommand = command;
		}
		private void SetUpsertPage(Page page)
		{
			_upsertPageCommand.Parameters["$work_id"].Value = page.WorkId;
			_upsertPageCommand.Parameters["$page_number"].Value = page.PageNumber;
			_upsertPageCommand.Parameters["$original_url"].Value = page.OriginalUrl;
			_upsertPageCommand.Parameters["$width"].Value = page.Width;
			_upsertPageCommand.Parameters["$height"].Value = page.Height;
			_upsertPageCommand.Parameters["$extension"].Value = page.Extension;
			_upsertPageCommand.Parameters["$filename"].Value = page.FileName;
		}
		#endregion

		#region Inserting Tags
		private SqliteCommand _insertTagCommand;
		private string InsertTag => @"
INSERT OR IGNORE INTO tags (id, name, public) VALUES ($id, $name, $public);
";
		private void GenerateInsertTagCommand()
		{
			var command = _connection.CreateCommand();
			command.CommandText = InsertTag;
			command.Parameters.Add("$id", SqliteType.Integer);
			command.Parameters.Add("$name", SqliteType.Text);
			command.Parameters.Add("$public", SqliteType.Integer);
			_insertTagCommand = command;
		}
		private void SetInsertTag(int id, string name, bool isPublic)
		{
			_insertTagCommand.Parameters["$id"].Value = id;
			_insertTagCommand.Parameters["$name"].Value = name;
			_insertTagCommand.Parameters["$public"].Value = isPublic;
		}
		private void SetInsertTag(string name, bool isPublic)
		{
			_insertTagCommand.Parameters["$id"].Value = DBNull.Value;
			_insertTagCommand.Parameters["$name"].Value = name;
			_insertTagCommand.Parameters["$public"].Value = isPublic;
		}
		#endregion

		#region Connecting Tags to Works
		private SqliteCommand _tagWorkCommand;
		private string InsertTagWork => @"
INSERT OR IGNORE INTO tags_works (work_id, tag_id) VALUES ($work_id, $tag_id);
";
		private void GenerateInsertTagWorkCommand()
		{
			var command = _connection.CreateCommand();
			command.CommandText = InsertTagWork;
			command.Parameters.Add("$work_id", SqliteType.Integer);
			command.Parameters.Add("$tag_id", SqliteType.Integer);
			_tagWorkCommand = command;
		}
		private void SetInsertTagWork(int workId, int tagId)
		{
			_tagWorkCommand.Parameters["$work_id"].Value = workId;
			_tagWorkCommand.Parameters["$tag_id"].Value = tagId;
		}
		#endregion

		#region Getting Works
		public string GetWorksStatement = @"
SELECT works.id, title, illust_type, user_id, user_name, bookmark_id, width, height, page_count, bookmarkable,
create_date, update_date, unlisted, bookmark_count, like_count, comment_count, view_count,
name, public
FROM works
JOIN tags_works ON work_id = works.id
JOIN tags ON tag_id = tags.id
";
		public string GetWorkStatement(int id)
        {
			return GetWorksStatement + $"WHERE works.id = {id}";
        }
		#endregion
		#endregion
	}
}
