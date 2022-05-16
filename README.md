# Pixiv Bookmark Viewer
Web-based browser interface for browsing your own pixiv bookmarks. Automatically downloads and archives existing bookmarks, and uses archived versions to display. 


# Running
Relies on [my pixiv API implementation](https://github.com/victoisu/pixiv-api-cs) as middleware. For the time being, to run this you'll need to compile a dll of it yourself.
Also currently requires a PHP session ID from a pixiv login (in lieu of taking username/password). This is supplied as a user secret under `Pixiv:PhpSession`.

This is very much not complete. 