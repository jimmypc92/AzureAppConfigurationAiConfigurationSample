using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AzureAppConfigurationChatBot
{
    /// <summary>
    /// This is a contrived authentication handler that authenticates a user based off of parameters passed in via the request's query string parameters.
    /// No secret exchange/verification is performed, so this handler should not be used in scenarios outside of this demo application.
    /// 
    /// To assign a user, use the following query string structure "?username=JohnDoe&groups=MyGroup1,MyGroup2"
    /// </summary>
    class QueryStringAuthenticationHandler(IOptionsMonitor<QueryStringAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<QueryStringAuthenticationOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity();

            //
            // Extract username
            if (Context.Request.Query.TryGetValue(Options.UsernameParameterName, out StringValues value))
            {
                string username = value.First();

                identity.AddClaim(new Claim(ClaimTypes.Name, username));

                Logger.LogInformation("Assigning the username {username} to the request.", username);
            }

            //
            // Extract groups
            if (Context.Request.Query.TryGetValue(Options.GroupsParameterName, out StringValues groupsValue))
            {
                IEnumerable<string> groups = groupsValue.First().Split(',').Select(g => g.Trim());

                foreach (string group in groups)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, group));
                }

                Logger.LogInformation("Assigning the following groups '{groups}' to the request.", string.Join(", ", groups));
            }

            //
            // Build principal and return result
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
    
    static class QueryStringAuthenticationExtensions
    {
        public static AuthenticationBuilder AddQueryString(this AuthenticationBuilder builder)
        {
            return builder.AddScheme<QueryStringAuthenticationOptions, QueryStringAuthenticationHandler>(Schemes.QueryString, null);
        }
    }
    
    class QueryStringAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string UsernameParameterName { get; set; } = "username";

        public string GroupsParameterName { get; set; } = "groups";
    }
    
    public class Schemes
    {
        public const string QueryString = "QueryString";
    }

}