﻿using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;
using Xunit.Abstractions;

namespace Knapcode.BlobDelta.Test.Functional
{
    public class BlobContainerEnumerableFacts
    {
        public class ImmediatelyReturnsFalseOnEmptyContainer : Test
        {
            public ImmediatelyReturnsFalseOnEmptyContainer(ITestOutputHelper output) : base(output)
            {
            }

            [Fact]
            public async Task Run()
            {
                var target = Create();

                var output = await target.ToListAsync();

                Assert.Empty(output);
            }
        }

        public class ReturnsAllBlobs : Test
        {
            public ReturnsAllBlobs(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            [InlineData(3)]
            [InlineData(4)]
            [InlineData(5000)]
            public async Task Run(int pageSize)
            {
                var names = new[] { "a", "b", "c" };
                await CreateBlockBlobsAsync(names);
                var target = Create(pageSize: pageSize);

                var output = await target.ToListAsync();

                Assert.Equal(names, output.GetBlobNames());
            }
        }

        public class ObservesMinBound : Test
        {
            public ObservesMinBound(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            [InlineData(5000)]
            public async Task Run(int pageSize)
            {
                await CreateBlockBlobsAsync("a", "b", "c");
                var target = Create(minBlobName: "b", pageSize: pageSize);

                var output = await target.ToListAsync();

                Assert.Equal(new[] { "b", "c" }, output.GetBlobNames());
            }
        }

        public class ObservesMaxBound : Test
        {
            public ObservesMaxBound(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            [InlineData(5000)]
            public async Task Run(int pageSize)
            {
                await CreateBlockBlobsAsync("a", "b", "c");
                var target = Create(maxBlobName: "c", pageSize: pageSize);

                var output = await target.ToListAsync();

                Assert.Equal(new[] { "a", "b" }, output.GetBlobNames());
            }
        }

        public class ObservesMinAndMaxBound : Test
        {
            public ObservesMinAndMaxBound(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            [InlineData(5000)]
            public async Task Run(int pageSize)
            {
                await CreateBlockBlobsAsync("a", "b", "c", "d");
                var target = Create(minBlobName: "b", maxBlobName: "d", pageSize: pageSize);

                var output = await target.ToListAsync();

                Assert.Equal(new[] { "b", "c" }, output.GetBlobNames());
            }
        }

        public class ObservesPrefixWithMatches : Test
        {
            public ObservesPrefixWithMatches(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            [InlineData(5000)]
            public async Task Run(int pageSize)
            {
                await CreateBlockBlobsAsync("1/a", "1/b", "2/a", "2/b", "3/a");
                var target = Create(prefix: "2/", pageSize: pageSize);

                var output = await target.ToListAsync();

                Assert.Equal(new[] { "2/a", "2/b" }, output.GetBlobNames());
            }
        }

        public class ObservesPrefixWithoutMatches : Test
        {
            public ObservesPrefixWithoutMatches(ITestOutputHelper output) : base(output)
            {
            }

            [Fact]
            public async Task Run()
            {
                await CreateBlockBlobsAsync("1/a");
                var target = Create(prefix: "2/");

                var output = await target.ToListAsync();

                Assert.Empty(output);
            }
        }

        public class UsesInitialContinuationToken : Test
        {
            public UsesInitialContinuationToken(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            [InlineData(5000)]
            public async Task Run(int pageSize)
            {
                await CreateBlockBlobsAsync("a", "b", "c", "d");
                var initial = Create(pageSize: 1);
                var initialEnumerator = initial.GetEnumerator();
                await initialEnumerator.SkipAsync(2);
                var target = Create(
                    initialContinuationToken: initialEnumerator.Current.ContinuationToken,
                    pageSize: pageSize);

                var output = await target.ToListAsync();

                Assert.Equal(new[] { "b", "c", "d" }, output.GetBlobNames());
            }
        }


        public class UsesInitialContinuationTokenWithPrefix : Test
        {
            public UsesInitialContinuationTokenWithPrefix(ITestOutputHelper output) : base(output)
            {
            }

            [Fact]
            public async Task Run()
            {
                await CreateBlockBlobsAsync("1/a", "2/b", "3/c", "3/d");
                var initial = Create(pageSize: 1);
                var initialEnumerator = initial.GetEnumerator();
                await initialEnumerator.SkipAsync(2);
                var target = Create(
                    initialContinuationToken: initialEnumerator.Current.ContinuationToken,
                    prefix: "3/");

                var output = await target.ToListAsync();

                Assert.Equal(new[] { "3/c", "3/d" }, output.GetBlobNames());
            }
        }

        public class Test : BaseFacts, IAsyncLifetime
        {
            public Test(ITestOutputHelper output) : base(output)
            {
                ContainerName = GetContainerName();
                Container = BlobClient.GetContainerReference(ContainerName);
            }

            public string ContainerName { get; }
            public CloudBlobContainer Container { get; }

            public BlobContainerEnumerable Create(
                BlobContinuationToken initialContinuationToken = null,
                string minBlobName = null,
                string maxBlobName = null,
                string prefix = null,
                int pageSize = 5000)
            {
                return new BlobContainerEnumerable(
                    Container,
                    initialContinuationToken,
                    minBlobName,
                    maxBlobName,
                    prefix,
                    pageSize);
            }

            public async Task CreateBlockBlobsAsync(params string[] names)
            {
                await CreateBlockBlobsAsync(Container, names);
            }

            public async Task CreateBlockBlobAsync(string name, string content = "")
            {
                await CreateBlockBlobAsync(Container, name, content);
            }

            public async Task DisposeAsync()
            {
                await DisposeAsync(Container);
            }

            public async Task InitializeAsync()
            {
                await InitializeAsync(Container);
            }
        }
    }
}
