# ![StirlingLabs.Tests](https://raw.githubusercontent.com/StirlingLabs/Tests.Net/main/Tests.Net.jpg)


StirlingLabs.Tests is a lightweight test framework built for simplicity. It's designed to be easy to use, with a minimalistic approach to testing that focuses on simplicity and ease-of-use.

## Features

* Lightweight and easy to use
* No need for attributes or class decorations
* Injection of `this` pointer, `TextWriter` for logging, and `CancellationToken` for test cancellation signal via method parameters
* Classes must be sealed and in a namespace that ends with `.Tests` or have a class name that ends with `Tests`
* Class constructors act as `OneTimeSetUp` equivalents

## Getting Started

To get started with StirlingLabs.Tests, simply install the NuGet package and add a reference to your test project. Then, define your tests as methods within a sealed class, in a namespace that ends with `.Tests` or has a class name that ends with `Tests`. Test methods may not be static.

```csharp
namespace MyProject.Tests
{
    public sealed class MyTests
    {
        public MyTests()
        {
            // OneTimeSetUp equivalent
        }

        public void MyTest1()
        {
            // test code
        }

        public void MyTest2(TextWriter logger)
        {
            // test code with logging
        }

        public void MyTest3(CancellationToken cancellationToken)
        {
            // test code with cancellation
        }

        public void MyTest4(TextWriter logger, CancellationToken cancellationToken)
        {
            // test code with logging and cancellation
        }
    }
}
```

Note that the parameters must be in the order specified, and may not be out of order.

The test method names must also be unique within the class. If you have multiple tests with the same name, you will get an error related to ambiguous method naming.

You can then run your tests using the built-in test runner, like this:

```shell
dotnet test MyProject.sln
```

## Contributing
We welcome contributions from the community! If you find a bug or have a feature request, please open an issue on GitHub. If you'd like to contribute code, please fork the repository and submit a pull request.

## License
This project is licensed under the MIT License - see the LICENSE file for details.

