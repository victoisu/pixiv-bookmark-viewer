using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Services
{
    public class FileService
    {
        private IWebHostEnvironment _env;
        private ConcurrentDictionary<string, bool> _busyPaths;
        private ConcurrentDictionary<string, bool> _savedPaths;
        private string PixivDir => Path.Combine(_env.ContentRootPath, "images", "pixiv");
        private string PixivOriginalDir => Path.Combine(PixivDir, "original");
        private string PixivThumbnailDir => Path.Combine(PixivDir, "thumbnails");
        private HashSet<string> _otherDirs = new();

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
            _busyPaths = new();
            _savedPaths = new();
            Directory.CreateDirectory(PixivDir);
            Directory.CreateDirectory(PixivOriginalDir);
            Directory.CreateDirectory(PixivThumbnailDir);
        }

        public void AddPixivOriginalDirectory(string path)
        {
            if (!Directory.Exists(path))
                throw new ArgumentException($"Path not found: {Path.GetFullPath(path)}");

            _otherDirs.Add(path);
        }

        public async Task<Stream> SavePixivOriginal(string fileName, Task<(Stream, string)> downloadTask)
        {
            var path = Path.Combine(PixivOriginalDir, fileName);
            await Take(path);
            try
            {
                var (image, _) = await downloadTask;
                if (Save(path))
                {
                    var saved = new FileStream(path, FileMode.Create);
                    image.CopyTo(saved);
                    saved.Close();
                }
                image.Position = 0;
                return image;
            }
            catch
            {
                Unsave(path);
                throw;
            }
            finally
            {
                Release(path);
            }
        }

        public async Task<(bool, Stream)> GetPixivOriginal(string fileName)
        {
            return await Get(PixivOriginalDir, fileName);
        }


        public string HavePixivOriginal(string fileName)
        {
            return Have(PixivOriginalDir, fileName);
        }

        public async Task<Stream> SavePixivThumbnail(string fileName, Stream thumbnail)
        {
            // TODO: DRY with SavePixivOriginal.
            var path = Path.Combine(PixivThumbnailDir, fileName);
            await Take(path);
            try
            {
                if (Save(path))
                {
                    var saved = new FileStream(path, FileMode.Create);
                    thumbnail.CopyTo(saved);
                    saved.Close();
                }
                thumbnail.Position = 0;
                return thumbnail;
            }
            catch
            {
                Unsave(path);
                throw;
            }
            finally
            {
                Release(path);
            }
        }

        public async Task<(bool, Stream)> GetPixivThumbnail(string fileName)
        {
            return await Get(PixivThumbnailDir, fileName);
        }

        public string HavePixivThumbnail(string fileName)
        {
            return Have(PixivThumbnailDir, fileName);
        }

        private string Have(string directory, string fileName)
        {
            string path = Path.Combine(directory, fileName);
            if (File.Exists(path))
                return path;

            foreach (var dir in _otherDirs)
            {
                path = Path.Combine(dir, fileName);
                if (File.Exists(path))
                    return path;
            }

            return string.Empty;
        }

        private async Task<(bool, Stream)> Get(string directory, string fileName)
        {
            string path = Have(directory, fileName);
            if (path == string.Empty)
                path = Path.Combine(directory, fileName);

            await Take(path);
            try
            {
                if (File.Exists(path))
                {
                    var mem = new MemoryStream();
                    var file = new FileStream(path, FileMode.Open);
                    file.CopyTo(mem);
                    mem.Position = 0;
                    file.Close();
                    return (true, mem);
                }
                else
                {
                    return (false, null);
                }
            }
            finally
            {
                Release(path);
            }
        }

        private async Task Take(string path)
        {
            await Task.Run(() => { while (!_busyPaths.TryAdd(path, true)) ; });
        }

        private void Release(string path)
        {
            _busyPaths.TryRemove(path, out var _);
        }

        private bool Save(string path)
        {
            return _savedPaths.TryAdd(path, true);
        }

        private void Unsave(string path)
        {
            _savedPaths.TryRemove(path, out var _);
        }
    }
}
