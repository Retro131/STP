using FluentAssertions;
using lab3;

namespace lab3Tests;

[TestFixture]
public class RepositoryTest
{

    [Test]
    public void Repository_AddUpdateRemoveClear()
    {
        var repository = new Repository<int>();

        repository.Size.Should().Be(0);

        repository.Add(10);
        repository.Add(20);
        repository.Size.Should().Be(2);
        repository.GetAll().Should().Equal(10, 20);

        repository.Update(0, 30);
        repository.GetAll().Should().Equal(30, 20);

        repository.Remove(0);
        repository.GetAll().Should().Equal(20);
        repository.Size.Should().Be(1);

        repository.Clear();
        repository.Size.Should().Be(0);
        repository.GetAll().Should().BeEmpty();
    }

    [Test]
    public void GetAll_ReturnsIndependentList()
    {
        var repository = new Repository<int>();
        repository.Add(10);

        var items = repository.GetAll();
        items.Clear();

        repository.GetAll().Should().Equal(10);
        repository.Size.Should().Be(1);
    }
}