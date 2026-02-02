using Domain.Interfaces;
using Domain.Results;
using Domain.Extensions;
using Moq;
namespace Assignment2.Tests.Domain.Extensions;

public class RepositoryExtensions_Tests
{
    private readonly Mock<IRepository<TestEntity>> _repoMock;
    public RepositoryExtensions_Tests()
    {
        _repoMock = new Mock<IRepository<TestEntity>>();
    }

    public class TestEntity
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    // Happy path: 
    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnExistingEntity_WhenMatchIsFound()
    {
        // ARRANGE:  
        TestEntity existing = new() { Id = "1", Name = "Banan" };

        _repoMock
            .Setup(mockRepo => mockRepo.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<TestEntity>>.OK(new List<TestEntity> { existing }));

        // ACT:
        RepositoryResult<TestEntity> result = await _repoMock.Object.GetOrCreateAsync(
            entity => entity.Name == "Banan",
            () => new TestEntity { Id = "1", Name = "Banan" },
            CancellationToken.None);

        // ASSERT: 
        Assert.True(result.Succeeded);
        Assert.Equal(existing, result.Data);

        _repoMock.Verify(mockRepo => mockRepo.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }



    // Happy path: 
    [Fact]
    public async Task GetOrCreateAsync_ShouldCreateEntity_WhenNoMatchIsFound()
    {
        // ARRANGE: 
        _repoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<TestEntity>>.OK(new List<TestEntity>()));

        _repoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // ACT: 
        RepositoryResult<TestEntity> result = await _repoMock.Object.GetOrCreateAsync(
            entity => entity.Name == "Banan",
            () => new TestEntity { Id = "1", Name = "Banan" },
            CancellationToken.None);

        // ASSERT: 
        Assert.True(result.Succeeded);
        Assert.Equal("Banan", result.Data!.Name);

        _repoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
    }



    // Negative case:
    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnError_WhenReadAsyncFails()
    {
        // ARRANGE: 
        _repoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<TestEntity>>.InternalServerError("Läsfel"));

        // ACT: 
        RepositoryResult<TestEntity> result = await _repoMock.Object.GetOrCreateAsync(
            entity => entity.Name == "Banan",
            () => new TestEntity { Id = "1", Name = "Banan" },
            CancellationToken.None);

        // ASSERT: 
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("Läsfel", result.ErrorMessage);

        _repoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
