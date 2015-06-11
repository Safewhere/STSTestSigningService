Introduction
    This is a test signing service that can be called by a user facing system, with a previously issued request security token response in base64 format as input,
    and the test signing service must then reply with an updated version in base64 format of the request security token response.
        - LifeTime
        - Id
        - Signature. Signing will be done correspondingly to the input token. In other words:
            + If the input token's is signed with sha1 algorithm, that of the updated token will be signed with sha1 algorithm either.
			+ It will do the same in case of sha256 algorithm
        - Encryption: No encryption for both input and output

Configuration
    - SigningCertificateThumbprint: thumbprint of a certificate with private key that is used to sign the updated token. The certificate must exist in LocalMachine\My.
    - serilog:minimum-level: specify the level of logging.  Log files are stored in the Logs\ folder.
    - owin:AutomaticAppStartup: tell the application that it should use OWIN middleware when hosting under IIS. This setting should be true.

Running
    The application can be run under IIS or Visual Studio 2013's IIS Express.

Unittest
    To run unittest, the stock certificate (CertificateIdp.p12) found in the Certificates folder must be imported to LocalMachine\My. Remember to grant access to it
    for the user that runs Visual Studio. Note that when you want to write test to run against a real site hosted under IIS, remember to grant access for the identity app pool account.
    Unittest has two main sets of test cases:
        - One set tests the TokenSigningService class.
        - One set tests against a real WebApi environment using OWIN.

How to call the service
    - Sample code which demonstrates how to call the service can be found in the SecurityTokenServiceTestSigningControllerTest class.

API Document
	This is using Json API schema, which is located at Content\api-docs.json, is based on Swagger 2.0 specifications. To use this API document:
	- Setup your application web service endpoint
	- Change your endpoint name in Content\api-docs.json at line "basePath": "/{application endpoint}/api" by replace {application endpoint} to your endpoint path.
	For instance: + if your service url is http://localhost:19000/api/SecurityTokenServiceTestSigning then the basePath will be "/api".
				  + if your service url is http://localhost:19000/kombit/api/SecurityTokenServiceTestSigning then the basePath will be "/kombit/api".
                  + if your service url is https://adgangsstyringeksempler.test-stoettesystemerne.dk/CHTestSigningService/ then the basePath will be "/api".

