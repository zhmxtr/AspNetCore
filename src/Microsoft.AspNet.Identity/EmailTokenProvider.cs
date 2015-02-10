using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    public class EmailTokenProviderOptions
    {
        public string Name { get; set; } = Resources.DefaultEmailTokenProviderName;

        public string MessageProvider { get; set; } = "Email";

        public string Subject { get; set; } = "Security Code";

        /// <summary>
        ///     Format string which will be used for the email body, it will be passed the token for the first parameter
        /// </summary>
        public string BodyFormat { get; set; } = "Your security code is: {0}";
    }

    /// <summary>
    ///     TokenProvider that generates tokens from the user's security stamp and notifies a user via their email
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class EmailTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
        where TUser : class
    {
        public EmailTokenProvider(IOptions<EmailTokenProviderOptions> options, string name = "")
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = options.GetNamedOptions(name);
        }

        public EmailTokenProviderOptions Options { get; private set; }

        public override string Name { get { return Options.Name; } }

        /// <summary>
        ///     True if the user has an email set
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return !string.IsNullOrWhiteSpace(email) && await manager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        ///     Returns the email of the user for entropy in the token
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager,
            TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return "Email:" + purpose + ":" + email;
        }

        /// <summary>
        ///     Notifies the user with a token via email using the Subject and BodyFormat
        /// </summary>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override async Task NotifyAsync(string token, UserManager<TUser> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            var msg = new IdentityMessage
            {
                Destination = await manager.GetEmailAsync(user),
                Subject = Options.Subject,
                Body = string.Format(CultureInfo.CurrentCulture, Options.BodyFormat, token)
            };
            await manager.SendMessageAsync(Options.MessageProvider, msg);
        }
    }
}