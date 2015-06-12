using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;
using System.Xml;

namespace Kombit.Samples.STSTestSigningService.Code
{
    public class CustomSaml2SecurityTokenHandler: Saml2SecurityTokenHandler
    {
        protected override Saml2Assertion ReadAssertion(XmlReader reader)
        {
            if (null == reader)
            {
                throw new ArgumentNullException("reader");
            }

            if (this.Configuration == null)
            {
                throw new ArgumentNullException("Configuration");
            }

            if (this.Configuration.IssuerTokenResolver == null)
            {
                throw new ArgumentNullException("IssuerTokenResolver");
            }

            if (this.Configuration.ServiceTokenResolver == null)
            {
                throw new ArgumentNullException("ServiceTokenResolver");
            }

            XmlDictionaryReader plaintextReader = XmlDictionaryReader.CreateDictionaryReader(reader);

            var assertion = new Saml2Assertion(new Saml2NameIdentifier("__TemporaryIssuer__"));

            // Throw if wrong element
            if (!plaintextReader.IsStartElement("Assertion", "urn:oasis:names:tc:SAML:2.0:assertion"))
            {
                plaintextReader.ReadStartElement("Assertion", "urn:oasis:names:tc:SAML:2.0:assertion");
            }

            // disallow empty
            if (plaintextReader.IsEmptyElement)
            {
                #pragma warning suppress 56504 // bogus - thinks plaintextReader.LocalName, plaintextReader.NamespaceURI need validation
                throw new Exception("Error ID3061: plaintextreader error thrown with localName =" + plaintextReader.LocalName + " and namespaceURI = " + plaintextReader.NamespaceURI);
            }

            // Construct a wrapped serializer so that the EnvelopedSignatureReader's 
            // attempt to read the <ds:KeyInfo> will hit our ReadKeyInfo virtual.
            var wrappedSerializer = new CustomWrappedSerializer(this, assertion);

            // SAML supports enveloped signature, so we need to wrap our reader.
            // We do not dispose this reader, since as a delegating reader it would
            // dispose the inner reader, which we don't properly own.
            var realReader = new EnvelopedSignatureReader(plaintextReader, wrappedSerializer, this.Configuration.IssuerTokenResolver, false, false, false);
            // Process @attributes
            string value;

            // @xsi:type
            XmlUtil.ValidateXsiType(realReader, "AssertionType", "urn:oasis:names:tc:SAML:2.0:assertion");

            // @Version - required - must be "2.0"
            string version = realReader.GetAttribute("Version");
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception("ID0001 error thrown, verion is null");
            }

            if (!StringComparer.Ordinal.Equals(assertion.Version, version))
            {
                throw new Exception("ID4100 error thrown, verion is not valid");
            }

            // @ID - required
            value = realReader.GetAttribute("ID");
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception("ID0001 error thrown, ID is null");
            }

            assertion.Id = new Saml2Id(value);

            // @IssueInstant - required
            value = realReader.GetAttribute("IssueInstant");
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception("ID0001 error thrown, IssueInstant is null");
            }

            assertion.IssueInstant = XmlConvert.ToDateTime(value, DateTimeFormats.Accepted);

            // Process <elements>
            realReader.Read();

            // <Issuer> 1
            assertion.Issuer = this.ReadIssuer(realReader);

            // <ds:Signature> 0-1
            realReader.TryReadSignature();

            // <Subject> 0-1
            if (realReader.IsStartElement("Subject", "urn:oasis:names:tc:SAML:2.0:assertion"))
            {
                assertion.Subject = this.ReadSubject(realReader);
            }

            // <Conditions> 0-1
            if (realReader.IsStartElement("Conditions", "urn:oasis:names:tc:SAML:2.0:assertion"))
            {
                assertion.Conditions = this.ReadConditions(realReader);
            }

            // <Advice> 0-1
            if (realReader.IsStartElement("Advice", "urn:oasis:names:tc:SAML:2.0:assertion"))
            {
                assertion.Advice = this.ReadAdvice(realReader);
            }

            // <Statement|AuthnStatement|AuthzDecisionStatement|AttributeStatement>, 0-OO
            while (realReader.IsStartElement())
            {
                Saml2Statement statement;

                if (realReader.IsStartElement("Statement", "urn:oasis:names:tc:SAML:2.0:assertion"))
                {
                    statement = this.ReadStatement(realReader);
                }
                else if (realReader.IsStartElement("AttributeStatement", "urn:oasis:names:tc:SAML:2.0:assertion"))
                {
                    statement = this.ReadAttributeStatement(realReader);
                }
                else if (realReader.IsStartElement("AuthnStatement", "urn:oasis:names:tc:SAML:2.0:assertion"))
                {
                    statement = this.ReadAuthenticationStatement(realReader);
                }
                else if (realReader.IsStartElement("AuthzDecisionStatement", "urn:oasis:names:tc:SAML:2.0:assertion"))
                {
                    statement = this.ReadAuthorizationDecisionStatement(realReader);
                }
                else
                {
                    break;
                }

                assertion.Statements.Add(statement);
            }

            if (null == assertion.Subject)
            {
                // An assertion with no statements MUST contain a <Subject> element. [Saml2Core, line 585]
                if (0 == assertion.Statements.Count)
                {
                    throw new Exception("ID4106 error thrown, Statements is empty");
                }

                // Furthermore, the built-in statement types all require the presence of a subject.
                // [Saml2Core, lines 1050, 1168, 1280]
                foreach (Saml2Statement statement in assertion.Statements)
                {
                    if (statement is Saml2AuthenticationStatement
                        || statement is Saml2AttributeStatement
                        || statement is Saml2AuthorizationDecisionStatement)
                    {
                        throw new Exception("ID4119 error thrown, invalid statement");
                    }
                }
            }

            // Reading the end element will complete the signature; 
            // capture the signing creds
            assertion.SigningCredentials = realReader.SigningCredentials;

            return assertion;
        }

        public SecurityKeyIdentifier CustomReadSigningKeyInfo(XmlReader reader, Saml2Assertion assertion)
        {
            return ReadSigningKeyInfo(reader, assertion);
        }

        public void CustomWriteSigningKeyInfo(XmlWriter writer, SecurityKeyIdentifier data)
        {
            WriteSigningKeyInfo(writer, data);
        }
    }
    class DateTimeFormats
    {
        internal static string[] Accepted = new string[] 
        {
                "yyyy-MM-ddTHH:mm:ss.fffffffZ",
                "yyyy-MM-ddTHH:mm:ss.ffffffZ",
                "yyyy-MM-ddTHH:mm:ss.fffffZ",
                "yyyy-MM-ddTHH:mm:ss.ffffZ",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "yyyy-MM-ddTHH:mm:ss.ffZ",
                "yyyy-MM-ddTHH:mm:ss.fZ",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
                "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
                "yyyy-MM-ddTHH:mm:ss.fffffzzz",
                "yyyy-MM-ddTHH:mm:ss.ffffzzz",
                "yyyy-MM-ddTHH:mm:ss.fffzzz",
                "yyyy-MM-ddTHH:mm:ss.ffzzz",
                "yyyy-MM-ddTHH:mm:ss.fzzz",
                "yyyy-MM-ddTHH:mm:sszzz"
        };

        internal static string Generated = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }
}