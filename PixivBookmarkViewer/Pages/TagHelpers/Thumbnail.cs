using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PixivBookmarkViewer.Pages.TagHelpers
{
    public class Thumbnail : TagHelper
    {
        public int Id { get; set; }
        public int Page { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            string classRes = "thumbnail";
            if (output.Attributes.TryGetAttribute("class", out var classAttr))
            {
                classRes += " " + classAttr.Value;
                output.Attributes.Remove(classAttr);
            }
            output.Attributes.Add("class", classRes);
            output.Attributes.Add("work-id", Id);
            output.Attributes.Add("page-number", Id);
            output.Content.AppendHtml($"<a href='api/Work/{Id}?page={Page}'><img class='thumbnail-img' src='/api/Work/{Id}/thumbnail?page={Page}'/></a>");
        }
    }
}
