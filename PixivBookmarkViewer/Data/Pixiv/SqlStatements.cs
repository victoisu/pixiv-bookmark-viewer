namespace PixivBookmarkViewer
{
	internal static class SqlStatements
    {
        public static string CreatePixivArchiveTables => @"
CREATE TABLE IF NOT EXISTS works (
	id INTEGER PRIMARY KEY,
	title TEXT,
	illust_type INTEGER,
	user_id INTEGER,
	user_name TEXT,
	bookmark_id INTEGER,
	width INTEGER,
	height INTEGER,
	page_count INTEGER,
	bookmarkable BOOLEAN,
	create_date DATETIME,
	update_date DATETIME,
	unlisted BOOLEAN,
	thumbnail TEXT
);

CREATE TABLE IF NOT EXISTS pages (
	work_id INTEGER,
	page_number INTEGER,
	original_url TEXT,
	width INTEGER,
	height INTEGER,
	extension TEXT,
	filename TEXT,
	PRIMARY KEY (work_id, page_number)
);

CREATE TABLE IF NOT EXISTS tags (
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	name TEXT,
	public BOOLEAN
);

CREATE TABLE IF NOT EXISTS tags_works (
	work_id INTEGER NOT NULL,
	tag_id INTEGER NOT NULL,
	PRIMARY KEY(work_id, tag_id)
);

CREATE INDEX idx_artist
ON works (user_id);
CREATE INDEX idx_illust_type
ON works (illust_type);
CREATE INDEX idx_tag
ON tags (name);
CREATE INDEX idx_fulltag
ON tags (name, public);
";
    }
}
