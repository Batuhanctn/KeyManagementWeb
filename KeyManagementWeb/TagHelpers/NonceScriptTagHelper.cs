using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Http;

namespace KeyManagementWeb.TagHelpers
{
    [HtmlTargetElement("script")]
    public class NonceScriptTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NonceScriptTagHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items["csp-nonce"] is string nonce)
            {
                output.Attributes.SetAttribute("nonce", nonce);
            }
        }
    }
}
