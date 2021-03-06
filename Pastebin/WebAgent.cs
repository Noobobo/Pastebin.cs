﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;

/// <summary>
/// The root namespace for all Pastebin API components.
/// </summary>
namespace Pastebin
{
    internal sealed class WebAgent
    {
        private const string ApiUrl = "http://pastebin.com/api/api_post.php";
        private const string LoginUrl = "http://pastebin.com/api/api_login.php";

        public readonly string apiKey;
        private string userKey;

        public bool Authenticated { get { return this.userKey != null; } }

        public WebAgent( string apiKey )
        {
            this.apiKey = apiKey;
        }

        public void Authenticate( string username, string password )
        {
            var parameters = new Dictionary<string, object>
            {
                { "api_user_name", username },
                { "api_user_password", password },
            };

            this.userKey = this.Post( parameters, LoginUrl );
        }

        public string Post( Dictionary<string, object> parameters, string url )
        {
            if( parameters == null )
                parameters = new Dictionary<string, object>();

            parameters.Add( "api_dev_key", this.apiKey );

            var pairs = new List<string>( parameters.Count );

            foreach( var pair in parameters )
                pairs.Add( String.Format( "{0}={1}", pair.Key, HttpUtility.UrlEncode( pair.Value.ToString() ) ) );

            var query = String.Join( "&", pairs );
            var request = WebRequest.Create( url );
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = Encoding.UTF8.GetByteCount( query );

            using( var stream = request.GetRequestStream() )
            {
                var buffer = Encoding.UTF8.GetBytes( query );
                stream.Write( buffer, 0, buffer.Length );
                stream.Flush();
            }

            var response = request.GetResponse();
            string text = null;

            using( var stream = response.GetResponseStream() )
            using( var reader = new StreamReader( stream, Encoding.UTF8 ) )
            {
                text = reader.ReadToEnd();
            }

            if( text.StartsWith( "Bad API request," ) )
                throw new PastebinException( text.Substring( text.IndexOf( ',' ) + 2 ) );

            return text;
        }

        public string Post( string option, Dictionary<string, object> parameters = null, bool authenticated = false )
        {
            if( parameters == null )
                parameters = new Dictionary<string, object>();

            parameters.Add( "api_option", option );

            if( authenticated && !this.Authenticated )
                throw new PastebinException( "User not logged in." );

            if( authenticated )
                parameters.Add( "api_user_key", this.userKey );

            return this.Post( parameters, ApiUrl );
        }

        public XDocument PostXml( string option, Dictionary<string, object> parameters = null, bool authenticated = false )
        {
            var xml = this.Post( option, parameters, authenticated );
            return XDocument.Parse( String.Format( "<?xml version='1.0' encoding='utf-8'?><result>{0}</result>", xml ) );
        }
    }
}
