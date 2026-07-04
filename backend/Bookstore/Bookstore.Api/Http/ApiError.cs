using System.Collections.Generic;

namespace Bookstore.Api.Http
{
    // Body shape every failed API response shares. Errors carries each
    // violation separately so a client can show them one by one; Message is
    // the joined summary, kept so clients reading a single string still work.
    public class ApiError
    {
        public string Message { get; set; }
        public IReadOnlyList<string> Errors { get; set; }

        public ApiError(IReadOnlyList<string> errors)
        {
            Errors = errors;
            Message = string.Join(" ", errors);
        }
    }
}
