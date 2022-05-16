using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using PixivBookmarkViewer.Search;
using PixivBookmarkViewer.Search.Logic;
using System.Collections.Concurrent;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PixivBookmarkViewer
{
	[Route("api/[controller]")]
	[ApiController]
	public class SearchController : ControllerBase
	{
		private DatabaseService _db;
		private PixivService _pixiv;
		private Random _rng = new();
		private FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

		public SearchController(DatabaseService db, PixivService pixiv)
		{
			_db = db;
			_pixiv = pixiv;
		}


		// GET api/<SearchController>
		[HttpGet]
		public List<FullWork> Get(string search, int count = 0, bool random = false)
		{
			search ??= "";
			List<FullWork> res = random ?
				_db.SearchWorks(search).OrderBy(_ => _rng.Next()).ToList() :
				_db.SearchWorks(search).ToList();

			if (res.Count == 0)
				return res;

			if (count != 0)
            {
				List<FullWork> trueRes = new();
                for (int i = 0; i < count; i++)
                {
					trueRes.Add(res[i % (res.Count)]);
                }
				res = trueRes;
            }

			return res;
		}

		[HttpGet("count")]
		public int GetCount(string search)
		{
			return Get(search).Count;
		}

		[HttpGet("tags")]
		public ActionResult GetPossibleTags(string search)
		{
			search ??= "";
			search += " FREE";
			var searchTerm = Parser.Parse(search).ConjunctiveNormal();
			var works = _db.Works;
			var res = new Dictionary<(string, bool), int>();
			var includedTally = new ConcurrentDictionary<(string, bool), int>();
			var excludedTally = new ConcurrentDictionary<(string, bool), int>();
			var tags = _db.Works.Tags;
			var remaining = 0;

			Parallel.ForEach(works, work => {
				var unsolved = ISearchTerm.PossibleTags(searchTerm, work, out var included, out var excluded);
				if (unsolved)
				{
					remaining++;
				}

				foreach (var tag in included)
				{
					includedTally[(tag.Name, tag.IsPublic)] = includedTally.GetValueOrDefault((tag.Name, tag.IsPublic)) + 1;
				}

				foreach (var tag in excluded)
				{
					excludedTally[(tag.Name, tag.IsPublic)] = excludedTally.GetValueOrDefault((tag.Name, tag.IsPublic)) + 1;
				}
			});

            foreach (var tag in tags)
            {
                res[tag] = includedTally.GetValueOrDefault(tag) + (remaining - excludedTally.GetValueOrDefault(tag));
            }

            return Ok(JsonSerializer.Serialize(res.OrderByDescending(x => x.Value).Select(x => new TagResult(x.Key.Item1, x.Value, x.Key.Item2)).ToList()));
		}

		private ActionResult GetPossibleTagsIntern(IEnumerable<FullWork> works)
		{
			var res = new Dictionary<(string, bool), int>();

			foreach (var work in works)
			{
				foreach (var tag in work.PublicTags)
				{
					res[(tag, true)] = res.GetValueOrDefault((tag, true)) + 1;
				}

				foreach (var tag in work.PersonalTags)
				{
					res[(tag, false)] = res.GetValueOrDefault((tag, false)) + 1;
				}
			}

			return Ok(JsonSerializer.Serialize(res.OrderByDescending(x => x.Value).Select(x => new TagResult(x.Key.Item1, x.Value, x.Key.Item2)).ToList()));
		}

		[HttpGet("random")]
		public async Task<IActionResult> GetRandomAsync(string search)
		{
			search ??= "";
			var works = Get(search);
			var work = works[_rng.Next(works.Count)];
			var pageNumber = _rng.Next(work.PageCount);

			var (img, name) = await _pixiv.GetImage(work.Id, pageNumber);

			if (_contentTypeProvider.TryGetContentType(name, out string contentType))
			{
				Response.Headers.Add("Content-Disposition", $"inline; filename={name}");

				return File(img, contentType);
			}
			else
			{
				return Redirect("~/images/static/500x500.png");
			}
		}

		public record TagResult
		{
			public string Tag { get; init; }
			public int Value { get; init; }
			public bool Public { get; init; }

			public TagResult(string t, int v, bool isPublic)
			{
				Tag = t;
				Value = v;
				Public = isPublic;
			}
		}
	}
}
