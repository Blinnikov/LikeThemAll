using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.ConfigurationModel;
using InstaSharp;
using Microsoft.AspNet.Http;
using InstaSharp.Models.Responses;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LikeThemAll.Controllers
{
    public class HomeController : Controller
    {
        private const string LocalhostIp = "127.0.0.1";
        private const string SessionKey = "InstaSharp.AuthInfo";
        private InstagramConfig _config;

        public HomeController(IConfiguration configuration)
        {
            var clientId = configuration.Get("InstaSharp:ClientId");
            var clientSecret = configuration.Get("InstaSharp:ClientSecret");
            var redirectUri = configuration.Get("InstaSharp:RedirectUri");

            _config = new InstagramConfig(clientId, clientSecret, redirectUri);
        }

        public IActionResult Index()
        {
            return View(3105);
        }

        public async Task<IActionResult> Instagram()
        {
            var oauthResponse = this.GetResponseFromSession();
            if (oauthResponse != null)
            {
                return await this.MakeRequest(oauthResponse);
            }

            var link = this.CreateAuthLink();
            return Redirect(link);
        }

        [Route("done")]
        public async Task<IActionResult> AuthDone(string code)
        {
            // add this code to the auth object
            var auth = new OAuth(_config);

            // now we have to call back to instagram and include the code they gave us
            // along with our client secret
            var oauthResponse = await auth.RequestToken(code);

            // both the client secret and the token are considered sensitive data, so we won't be
            // sending them back to the browser. we'll only store them temporarily.  If a user's session times
            // out, they will have to click on the authenticate button again - sorry bout yer luck.
            var oauthResponseSerialized = JsonConvert.SerializeObject(oauthResponse);
            this.Context.Session.SetString(SessionKey, oauthResponseSerialized);
            return RedirectToAction("Index");
        }

        private async Task<IActionResult> MakeRequest(OAuthResponse oauthResponse)
        {
            int statusCode = 200;
            var tags = new InstaSharp.Endpoints.Tags(_config, oauthResponse);
            tags.EnableEnforceSignedHeader(LocalhostIp);

            var likes = new InstaSharp.Endpoints.Likes(_config, oauthResponse);
            likes.EnableEnforceSignedHeader(LocalhostIp);


            var photos = await tags.Recent("foundyourbook");

            foreach (var photo in photos.Data)
            {
                if (!(photo.UserHasLiked ?? false))
                {
                    Thread.Sleep(1200);
                    var likeResult = await likes.Post(photo.Id);
                    statusCode = (int)likeResult.Meta.Code;
                    if (statusCode == 429)
                    {
                        break;
                    }
                }
            }

            return View("index", statusCode);
        }

        private string CreateAuthLink()
        {
            var scopes = new List<OAuth.Scope>();
            scopes.Add(OAuth.Scope.Likes);
            ////scopes.Add(OAuth.Scope.Comments);

            return OAuth.AuthLink(_config.OAuthUri + "/authorize", _config.ClientId, _config.RedirectUri, scopes, OAuth.ResponseType.Code);
        }

        private OAuthResponse GetResponseFromSession()
        {
            OAuthResponse oauthResponse = null;
            var sessionValue = this.Context.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(sessionValue))
            {
                oauthResponse = JsonConvert.DeserializeObject<OAuthResponse>(sessionValue);
            }

            return oauthResponse;
        }
    }
}