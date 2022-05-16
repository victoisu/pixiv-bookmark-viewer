using Microsoft.Data.Sqlite;

namespace PixivBookmarkViewer.Data
{
	public record DatabaseTag : Tag
	{
        public int Id { get; init; }

		public static DatabaseTag FromReader(SqliteDataReader reader, int index = 0)
		{
			return new DatabaseTag
			{
				Id = reader.GetInt32(index++),
				Name = reader.GetString(index++),
				IsPublic = reader.GetBoolean(index++)
			};
		}
	}
}
