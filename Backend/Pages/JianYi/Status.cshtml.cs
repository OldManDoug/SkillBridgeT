using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkillBridge.Web.Pages;

// This page only displays errors. It must also display a rejected POST whose token was invalid.
[IgnoreAntiforgeryToken]
public sealed class StatusModel : PageModel
{
    public int ResponseStatusCode { get; private set; }
    public string Heading => ResponseStatusCode switch
    {
        404 => "We could not find that page",
        429 => "Too many requests",
        _ => "We could not complete that request"
    };
    public string Message => ResponseStatusCode switch
    {
        400 => "The form could not be verified. Reload the page and try again.",
        429 => "Please wait a minute before trying to sign in or create an account again.",
        _ => "Return to a familiar page and try again."
    };

    public void OnGet(int code) => SetStatus(code);

    public void OnPost(int code) => SetStatus(code);

    private void SetStatus(int code)
    {
        ResponseStatusCode = code is >= 400 and <= 599 ? code : 404;
        Response.StatusCode = ResponseStatusCode;
    }
}
