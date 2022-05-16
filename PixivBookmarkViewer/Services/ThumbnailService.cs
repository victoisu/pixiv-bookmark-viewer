using PixivBookmarkViewer.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Services
{
    public class ThumbnailService
    {
        private PixivService _pixiv;
        private FileService _files;
        private const int _squareSide = 250;

        public ThumbnailService(PixivService pixiv, FileService files)
        {
            _pixiv = pixiv;
            _files = files;
        }

        public async Task<(Stream, string)> GetThumbnail(int id, int page = 0)
        {
            return await GetThumbnail(await _pixiv.GetPage(id, page));
        }

        public async Task<(Stream, string)> GetThumbnail(Page page)
        {
            var (have, stream) = await _files.GetPixivThumbnail(page.ThumbnailName);
            if (have)
                return (stream, page.ThumbnailName);

            return await DownloadThumbnail(page);
        }

        public async Task<(Stream, string)> DownloadThumbnail(int id, int page = 0)
        {
            return await DownloadThumbnail(await _pixiv.GetPage(id, page));
        }

        public async Task<(Stream, string)> DownloadThumbnail(Page page)
        {
            var (stream, _) = await _pixiv.GetImage(page);
            var thumbnail = GenerateThumbnail(stream);
            await _files.SavePixivThumbnail(page.ThumbnailName, thumbnail);
            return (thumbnail, page.ThumbnailName);
        }

        private static Stream GenerateThumbnail(Stream original)
        {
            Image originalImage = Image.FromStream(original);
            int width = originalImage.Width < originalImage.Height ? _squareSide : originalImage.Width;
            int height = originalImage.Height < originalImage.Width ? _squareSide : originalImage.Height;
            float ratio = originalImage.Width / (float)originalImage.Height;
            var sourceRect = new Rectangle(0, 0, _squareSide, _squareSide);

            Size s;
            if (originalImage.Width < originalImage.Height)
            {
                s = new Size(_squareSide, (int)(_squareSide / ratio));
            }
            else
            {
                s = new Size((int)(_squareSide * ratio), _squareSide);
            }

            var res = new Bitmap(originalImage, s);
            var stream = new MemoryStream();
            res.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            stream.Position = 0;

            return stream;
        }
    }
}
