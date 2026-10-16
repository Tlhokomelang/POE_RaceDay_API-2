# POE_RaceDay_API

## 1. Project Overview

POE_RaceDay_API is an ASP.NET Core Web API project developed using C#. The API provide a backend for managing race day-related information and processes.

The projects includes API controlled to handled requesting and responded, as well as JWT (JSON Web Token) authentication to help secure access to protected resources.

## 2. Technologies Used

- C#
- ASP.NET Core Web API
- .NET
- JWT Authentication
- RESTful API principles
- Unit Testing
- GitHub Actions for CI/CD

## 3. Project Features

The project is designed to support the following features:

- Processing HTTP requests through API controllers.
- Managing race day-related data.
- Validating incoming request.
- Securing protected endpoint using JWT authentication.
- Handling error and returning appropriate HTTP response.
- Testing controller functionality through unit tests.
- Automating build and test processes using GitHub Action.

*Note: The exact features depend on the functionality implemented in the project's controllers.*

## 4. Prerequisites

Before running the project, ensure that you have installed:

- .NET SDK compatible with the project.
- Visual Studio, Visual Studio Code, or another compatible C# IDE.
- Git.
- A GitHub account for repository hosting and CI/CD.

## 5. Installation and Setup

Follow these steps to set up the project locally.

### Step 1: Clone the repository

Open a terminal and run:

```bash
git clone <YOUR_GITHUB_REPOSITORY_URL>
```

Navigate to the project directory:

```bash
cd POE_RaceDay_API
```

Replace the repository URL with your actual GitHub repository URL.

### Step 2: Restore dependencies

Run the following command from the directory containing the API project file:

```bash
dotnet restore
```

### Step 3: Build the project

Compile the project to check for errors:

```bash
dotnet build
```

### Step 4: Run the API

Starts the application using:

```bash
dotnet run
```

The terminal will displays the local URL where the API is running. Use the displayed address to access the API.

## 6. Authentication

The API uses JWT authentication to authenticate requests to protected endpoints.

To access a protected endpoint:

1. Obtain a valid JWT token using the authentication functionality provided by the project.
2. Copy the token.
3. Include the token in the HTTP request's Authorization header.
4. Use the Bearer authentication scheme.

Example:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

Requests without a valid token may be rejected by protected endpoints.

**Security note:** Keep secret keys, passwords, database credentials, and other sensitive configuration values out of source control.

## 7. API Testing

The API can be tested using a suitable API testing tool, such as Swagger UI, Postman, or an equivalent tool, if configured for the project.

Testing should cover:

- Successful API requests.
- Invalid or missing input.
- Expected HTTP status codes.
- Authentication and authorization.
- Error handling.
- Access to protected endpoints with valid and invalid tokens.

The available endpoints and their HTTP methods should be confirmed from the project's controllers and API configuration.

## 8. Unit Testing

Unit tests are used to verify that individual component of the application behave as expected.

The tests for this project should cover the four supplied API controllers and relevant authentication configuration behaviour, where applicable.

Examples of areas to test include:

- Valid controller requests.
- Invalid input and validation errors.
- Expected responses and status codes.
- Error-handling scenarios.
- Authentication-related behaviour.

To run the unit test, navigating to the solution directory and executes:

```bash
dotnet test
```

Ensure that the test project is included in the solution and that the required test packages are installed.

## 9. Continuous Integration and Continuous Delivery (CI/CD)

GitHub Actions is used to automate the project's build and testing processes.

The workflow should be stored in the following directory:

```text
.github/workflows/
```

The workflow can be configured to run when changed are pushed to the repository or when a pull request is created.

A typical CI workflow performs these steps:

1. Checks out the repository.
2. Installs the required .NET SDK.
3. Restores project dependencies.
4. Builds the application.
5. Runs the unit tests.
6. Reports whether the build and tests passed or failed.

The workflow should use the .NET version specified by the project and should run the tests against the correct test project or solution.

## 10. Project Structure

A typical project structure may look like this:

```text
POE_RaceDay_API/
├── Controllers/
├── Properties/
├── Program.cs
├── appsettings.json
├── POE_RaceDay_API.csproj
├── Tests/
├── .github/
│   └── workflows/
│       └── dotnet.yml
└── README.md
```

The actual folder and file names may differ depending on the structure of the repository. Keep the existing project structure when adding the README.

## 11. Troubleshooting

**The project does not build**

- Check that the correct .NET SDK is installed.
- Run `dotnet restore`.
- Run `dotnet build` and review any error messages.

**The unit tests do not run**

- Confirm that the test project exists.
- Check that the test framework and required packages are installed.
- Run `dotnet test` from the solution directory.
- Review any failed tests and their error messages.

**Authentication fails**

- Check that the JWT configuration is correct.
- Confirm that the token is valid and has not expired.
- Verify that the request includes the correct Authorization header.
- Confirm that the endpoint requires the supplied authentication scheme.

**GitHub Actions fails**

- Open the repository's Actions tab on GitHub.
- Select the failed workflow run.
- Review the logs to identify the failing build or test step.

## 12. Conclusion

POE_RaceDay_API demonstrates the development of an ASP.NET Core Web API using C#, JWT authentication, controller-based request handling, unit testing, and automated build and test workflows.

The project can be further improved by expanding test coverage, strengthening security, improving error handling, and maintaining reliable CI/CD automation.
