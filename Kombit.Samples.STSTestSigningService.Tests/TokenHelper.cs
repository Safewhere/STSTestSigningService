#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using dk.nita.saml20;
using dk.nita.saml20.Schema.Core;
using dk.nita.saml20.Schema.Protocol;
using Kombit.Samples.STSTestSigningService.Code;
using Xunit;

#endregion

namespace Kombit.Samples.STSTestSigningService.Tests
{
    /// <summary>
    ///     A helper to specify if the test result is corrected or not
    /// </summary>
    public class TokenHelper
    {
        public static void AssertUpdatedToken(string updatedToken, X509Certificate2 certificate,
            string originalTokenMessage)
        {
            string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(originalTokenMessage));
            var originRstr = TokenSigningService.DeserializeRstrFromXml(decodedMessage);
            var originalAssertion = GetAssertion(decodedMessage);

            string decodedUpdatedToken = Encoding.UTF8.GetString(Convert.FromBase64String(updatedToken));
            var updatedRstr = TokenSigningService.DeserializeRstrFromXml(decodedUpdatedToken);

            Saml20Assertion updatedAssertion;
            bool validSignature = ValidateSignature(decodedUpdatedToken, certificate, out updatedAssertion);

            Assert.Equal(originRstr.Status, updatedRstr.Status);
            Assert.NotEqual(originRstr.AppliesTo, updatedRstr.AppliesTo);
            Assert.NotEqual(originRstr.Lifetime.Created, updatedRstr.Lifetime.Created);
            Assert.True(DateTime.UtcNow.Subtract(updatedRstr.Lifetime.Created.Value).TotalMinutes < 1);
            Assert.True(validSignature);

            AssertAssertionAttributes(originalAssertion.Assertion, updatedAssertion.Assertion);
        }

        /// <summary>
        ///     A method to verify if the signature is correct or not
        /// </summary>
        /// <param name="decodedUpdatedToken"></param>
        /// <param name="signingCertificate"></param>
        /// <param name="saml20Assertion"></param>
        /// <returns></returns>
        private static bool ValidateSignature(string decodedUpdatedToken, X509Certificate2 signingCertificate,
            out Saml20Assertion saml20Assertion)
        {
            saml20Assertion = GetAssertion(decodedUpdatedToken);
            bool validSignature = saml20Assertion.CheckSignature(new List<AsymmetricAlgorithm>
            {
                signingCertificate.PrivateKey
            });
            return validSignature;
        }

        /// <summary>
        ///     A method to extract assertion from the updated token
        /// </summary>
        /// <param name="decodedUpdatedToken"></param>
        /// <returns></returns>
        private static Saml20Assertion GetAssertion(string decodedUpdatedToken)
        {
            Saml20Assertion saml20Assertion;
            XmlDocument doc = new XmlDocument {XmlResolver = null, PreserveWhitespace = true};

            doc.LoadXml(decodedUpdatedToken);
            bool isEncrypted;
            XmlElement assertionElement = GetAssertion(doc.DocumentElement, out isEncrypted);
            Assert.False(isEncrypted, "Assertion must not be encrypted.");
            saml20Assertion = new Saml20Assertion(assertionElement, null, AssertionProfile.DKSaml, false,
                false);
            return saml20Assertion;
        }

        /// <summary>
        ///     a method to get assertion from an xml element
        /// </summary>
        /// <param name="el"></param>
        /// <param name="isEncrypted"></param>
        /// <returns></returns>
        private static XmlElement GetAssertion(XmlElement el, out bool isEncrypted)
        {
            XmlNodeList encryptedList =
                el.GetElementsByTagName(EncryptedAssertion.ELEMENT_NAME, Saml20Constants.ASSERTION);

            if (encryptedList.Count == 1)
            {
                isEncrypted = true;
                return (XmlElement) encryptedList[0];
            }

            XmlNodeList assertionList =
                el.GetElementsByTagName(Assertion.ELEMENT_NAME, Saml20Constants.ASSERTION);

            if (assertionList.Count == 1)
            {
                isEncrypted = false;
                return (XmlElement) assertionList[0];
            }

            isEncrypted = false;
            return null;
        }

        /// <summary>
        ///     A method to specify if the updated assertion is correct
        /// </summary>
        /// <param name="originalAssertion"></param>
        /// <param name="updatedAssertion"></param>
        private static void AssertAssertionAttributes(Assertion originalAssertion, Assertion updatedAssertion)
        {
            Assert.NotEqual(originalAssertion.ID, updatedAssertion.ID);
            Assert.NotEqual(originalAssertion.IssueInstant, updatedAssertion.IssueInstant);
            Assert.True(DateTime.UtcNow.Subtract(updatedAssertion.IssueInstant.Value).TotalMinutes < 1);

            Assert.NotEqual(originalAssertion.Conditions.NotBefore, updatedAssertion.Conditions.NotBefore);
            Assert.NotEqual(originalAssertion.Conditions.NotOnOrAfter, updatedAssertion.Conditions.NotOnOrAfter);
            Assert.True(DateTime.UtcNow.Subtract(updatedAssertion.Conditions.NotOnOrAfter.Value).TotalMinutes < 1);

            SubjectConfirmation originalSubjectConfirmation =
                originalAssertion.Subject.Items.OfType<SubjectConfirmation>().First();
            SubjectConfirmation updatedSubjectConfirmation =
                updatedAssertion.Subject.Items.OfType<SubjectConfirmation>().First();
            Assert.NotEqual(originalSubjectConfirmation.SubjectConfirmationData.NotOnOrAfter,
                updatedSubjectConfirmation.SubjectConfirmationData.NotOnOrAfter);
            Assert.True(
                DateTime.UtcNow.Subtract(updatedSubjectConfirmation.SubjectConfirmationData.NotOnOrAfter.Value)
                    .TotalMinutes < 1);

            AuthnStatement originalAuthnStatement = originalAssertion.Items.OfType<AuthnStatement>().First();
            AuthnStatement updatedAuthnStatement = updatedAssertion.Items.OfType<AuthnStatement>().First();
            Assert.NotEqual(originalAuthnStatement.AuthnInstant, updatedAuthnStatement.AuthnInstant);
            Assert.True(DateTime.UtcNow.Subtract(updatedAuthnStatement.AuthnInstant.Value).TotalMinutes < 1);
        }
    }
}