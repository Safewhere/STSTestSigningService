using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Policy;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Kombit.Samples.Common;

namespace Kombit.Samples.STSTestSigningService.Code
{
    /// <summary>
    ///     A class derivered from ITokenSigningService which is to implement all attrbutes, methods used in token signing
    ///     service
    /// </summary>
    public class TokenSigningService : ITokenSigningService
    {
        /// <summary>
        ///     Update the request security token response. Includes: Includes signature, NotBefore, NotOnOrAfter
        /// </summary>
        /// <param name="certificate">the new signing certificate to update rstr</param>
        /// <param name="encodedRstr">the Rstr xml string in url-encoded base64 format</param>
        /// <returns>the update rstr xml string in url-encoded base64 format</returns>
        public string UpdateToken(string encodedRstr, X509Certificate2 certificate)
        {
            if (encodedRstr == null)
                throw new ArgumentNullException("encodedRstr");
            if (certificate == null)
                throw new ArgumentNullException("certificate");
            if (!certificate.HasPrivateKey)
                throw new ArgumentException("The certificate that is used for updating a token must have private key.",
                    "certificate");

            string decoded = GetBase64DecodedRstResponse(encodedRstr, Encoding.UTF8);
            var originRstr = DeserializeRstrFromXml(decoded);

            var updatedToken = UpdateRstr(originRstr, certificate);
            return GetBase64EncodedXmlResponse(SerializeRstr(updatedToken), Encoding.UTF8);
        }

        /// <summary>
        ///     Get request security token response string from a base64 decoded value
        /// </summary>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static string GetBase64DecodedRstResponse(string message, Encoding encoding)
        {
            string samlResponse = encoding.GetString(Convert.FromBase64String(message));
            Logging.Instance.Debug("Decoded token: " + samlResponse);
            return samlResponse;
        }

        /// <summary>
        ///     Get a base64 encoded xml Rsrs from a string
        /// </summary>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static string GetBase64EncodedXmlResponse(string message, Encoding encoding)
        {
            string samlResponse = Convert.ToBase64String(encoding.GetBytes(message));
            Logging.Instance.Debug("Encoded token: " + samlResponse);
            return samlResponse;
        }

        /// <summary>
        ///     Serialize an object RSTR to a string
        /// </summary>
        /// <param name="rstr"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "This is a false warning. There is nothing wrong with use StringWriter like that. Perhaps it might be for some other poorly implemented types.")]
        private static string SerializeRstr(RequestSecurityTokenResponse rstr)
        {
            var wsResponseSerializer = new WSTrust13ResponseSerializer();
            using (var sw = new StringWriter())
            {
                using (var xw = XmlWriter.Create(sw))
                {
                    wsResponseSerializer.WriteXml(rstr, xw, new WSTrustSerializationContext());
                }
                return sw.ToString();
            }
        }

        /// <summary>
        ///     Convert an xml string to an xml element
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private static XmlElement GetElement(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        /// <summary>
        ///     Update a rstr object with new certificate
        /// </summary>
        /// <param name="originRstr"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private RequestSecurityTokenResponse UpdateRstr(RequestSecurityTokenResponse originRstr,
            X509Certificate2 certificate)
        {
            var updatedRstr = originRstr;
            //update its lifetime
            updatedRstr.Lifetime = new Lifetime(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
            var originSaml2Token = originRstr.RequestedSecurityToken.SecurityToken as Saml2SecurityToken;
            if (originSaml2Token == null)
            {
                Logging.Instance.Error("security token is a not a saml2 security token");
                return null;
            }
            string signatureAlgorithm = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            // sha256 is "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"
            string digestAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1";
            // sha256 is "http://www.w3.org/2001/04/xmlenc#sha256"
            var originSigningCredentials = originSaml2Token.Assertion.SigningCredentials;

            if (originSigningCredentials != null)
            {
                signatureAlgorithm = originSigningCredentials.SignatureAlgorithm;
                digestAlgorithm = originSigningCredentials.DigestAlgorithm;
            }

            Logging.Instance.Debug(
                "Signing algorithm is {SignatureAlgorithm} and digest algorithm is {DigestAlgorithm}",
                signatureAlgorithm, digestAlgorithm);

            var signingCredential = new X509SigningCredentials(certificate, signatureAlgorithm, digestAlgorithm);
            //initialize a new saml2 assertion with a proper signing credential
            var saml2Assertion = new Saml2Assertion(originSaml2Token.Assertion.Issuer)
            {
                SigningCredentials = signingCredential,
                Subject = originSaml2Token.Assertion.Subject,
                Conditions = originSaml2Token.Assertion.Conditions,
            };
            //Update token's life time condition
            saml2Assertion.Conditions.NotOnOrAfter = DateTime.UtcNow.AddHours(1);
            saml2Assertion.Conditions.NotBefore = DateTime.UtcNow;
            if (saml2Assertion.Subject.SubjectConfirmations != null)
            {
                foreach (var saml2SubjectConfirmation in saml2Assertion.Subject.SubjectConfirmations)
                {
                    if (saml2SubjectConfirmation.SubjectConfirmationData != null)
                    {
                        saml2SubjectConfirmation.SubjectConfirmationData.NotOnOrAfter = DateTime.UtcNow.AddHours(1);
                        saml2SubjectConfirmation.SubjectConfirmationData.NotBefore = DateTime.UtcNow;
                    }
                }
            }
            foreach (var statement in originSaml2Token.Assertion.Statements)
            {
                Saml2AuthenticationStatement saml2AuthenticationStatement = statement as Saml2AuthenticationStatement;
                if (saml2AuthenticationStatement != null)
                {
                    saml2AuthenticationStatement.AuthenticationInstant = DateTime.UtcNow;
                }
                saml2Assertion.Statements.Add(statement);
            }

            var saml2Token = new Saml2SecurityToken(saml2Assertion);

            updatedRstr.RequestedSecurityToken = new RequestedSecurityToken(GetElement(SerializeToken(saml2Token)));

            return updatedRstr;
        }

        /// <summary>
        ///     Convert a generic xml security token to Saml security token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static SecurityToken ToSamlSecurityToken(GenericXmlSecurityToken token)
        {
            var handlers = SecurityTokenHandlerCollection.CreateDefaultSecurityTokenHandlerCollection();
            var saml2Handler = new CustomSaml2SecurityTokenHandler();
            handlers.AddOrReplace(saml2Handler);
            var reader = new XmlTextReader(new StringReader(token.TokenXml.OuterXml));
            return handlers.ReadToken(reader);
        }

        /// <summary>
        ///     Reserialize a string to a request security token response object
        /// </summary>
        /// <param name="rstsString"></param>
        /// <returns></returns>
        public static RequestSecurityTokenResponse DeserializeRstrFromXml(string rstsString)
        {
            var wsResponseSerializer = new WSTrust13ResponseSerializer();
            XmlReader xmlReader = XmlReader.Create(new StringReader(rstsString));
            var rstr = wsResponseSerializer.ReadXml(xmlReader, new WSTrustSerializationContext());
            SecurityToken proofKey = null;
            if (rstr.RequestedProofToken != null)
            {
                var proofKeyBytes = rstr.RequestedProofToken.ProtectedKey.GetKeyBytes();
                if (proofKeyBytes != null)
                    proofKey = new BinarySecretSecurityToken(proofKeyBytes);
            }

            var securityToken = ConvertRstrToGenericXmlSecurityToken(rstr, proofKey);

            rstr.RequestedSecurityToken = new RequestedSecurityToken(ToSamlSecurityToken(securityToken));
            return rstr;
        }

        /// <summary>
        /// Serializes a saml2 token to xml string
        /// </summary>
        /// <returns></returns>
        private static string SerializeToken(SecurityToken token)
        {
            var handlers = SecurityTokenHandlerCollection.CreateDefaultSecurityTokenHandlerCollection();

            StringBuilder sb = new StringBuilder();
            handlers.WriteToken(new XmlTextWriter(new StringWriter(sb)), token);
            return sb.ToString();
        }

        /// <summary>
        /// Converts Rstr to GenericXmlSecurityToken what will then be pushed back to a requested security token.
        /// </summary>
        /// <param name="rstr"></param>
        /// <param name="proofKey"></param>
        /// <returns></returns>
        private static GenericXmlSecurityToken ConvertRstrToGenericXmlSecurityToken(RequestSecurityTokenResponse rstr, SecurityToken proofKey)
        {
            DateTime created = DateTime.UtcNow;
            DateTime expires = DateTime.UtcNow.AddHours(1);
            if (rstr.Lifetime != null)
            {
                if (rstr.Lifetime.Created.HasValue)
                {
                    created = rstr.Lifetime.Created.Value;
                }
                if (rstr.Lifetime.Expires.HasValue)
                {
                    expires = rstr.Lifetime.Expires.Value;
                }
            }

            return new GenericXmlSecurityToken(ExtractTokenXml(rstr), proofKey, created, expires,
                rstr.RequestedAttachedReference, rstr.RequestedUnattachedReference,
                new ReadOnlyCollection<IAuthorizationPolicy>(new List<IAuthorizationPolicy>()));
        }

        /// <summary>
        /// Extracts security token from a RequestedSecurityToken in xml format
        /// </summary>
        /// <param name="rstr"></param>
        /// <returns></returns>
        private static XmlElement ExtractTokenXml(RequestSecurityTokenResponse rstr)
        {
            if (rstr.RequestedSecurityToken.SecurityToken != null)
                return ConvertXElementToXmlElement(XElement.Parse(SerializeToken(rstr.RequestedSecurityToken.SecurityToken)));
            return rstr.RequestedSecurityToken.SecurityTokenXml;
        }

        /// <summary>
        /// Converts XElement to XmlElement (source: https://gist.github.com/rarous/3150395)
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        public static XmlElement ConvertXElementToXmlElement(XElement el)
        {
            using (var reader = el.CreateReader())
            {
                var doc = new XmlDocument();
                doc.Load(reader);
                return doc.DocumentElement;
            }
        }
    }
}