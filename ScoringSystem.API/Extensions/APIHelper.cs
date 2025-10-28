using Blazored.LocalStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using ScoringSystem.API.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Application.Common
{
    public class APIHelper
    {

        public async Task<RestClient> GetClient()
        {

            var token = AuthContext.Token;
            var options = new RestClientOptions("https://localhost:5000")
            {
                Authenticator = token != "" && token != null ? new JwtAuthenticator(token) : null
            };
            var client = new RestClient(options);

            return client;
        }
        
    }
}
