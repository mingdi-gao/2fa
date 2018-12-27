using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Google.Authenticator;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using OwinTwilio.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace OwinTwilio
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        const string accountSid = "ACe405a687c651f4ebd6ac3d476d675a60";
        const string authToken = "a65fe71f3c914a170326ed06e300869e";
        const string key = "12345!@#"; // 10-12 char string as private key in google authenticator

        public Task SendAsync(IdentityMessage message)
        {
            // My SMS service here to send a text message.
            TwilioClient.Init(accountSid, authToken);
            var SMSMessage = MessageResource.CreateAsync(
                body: "Stephen's Testing Twilio: \n" + message.Body,
                from: new Twilio.Types.PhoneNumber("+19705148522"),
                to: new Twilio.Types.PhoneNumber(message.Destination)
            );

            return Task.FromResult(0);
        }

        public Task SendTOTPAsync(string destination)
        {
            // My SMS service to Sent TOT token
            string UserUniqueKey = "user" + key;
            TwoFactorAuthenticator tfaClient = new TwoFactorAuthenticator();
            var setupInfo = tfaClient.GenerateSetupCode("BrownGreer LLC", "BG Developer", UserUniqueKey, 300, 300);
            string manualKey = setupInfo.ManualEntryKey;
            string QRCodeUrl = setupInfo.QrCodeSetupImageUrl;
            TwilioClient.Init(accountSid, authToken);

            var SMSMessage = MessageResource.CreateAsync(
                body: "Stephen's Testing Twilio. ManualKey is " + manualKey + ", barcodeURL is " + QRCodeUrl,
                from: new Twilio.Types.PhoneNumber("+19705148522"),
                to: new Twilio.Types.PhoneNumber(destination)
            );
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
