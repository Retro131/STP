using FluentAssertions;
using FluentAssertions.Execution;
using STP.Lab2;
using STP.Lab2.Models;

namespace STPTests;

[TestFixture]
public class PlaylistTests
{
    private static Track MakeTrack(int id)
    {
        return new Track($"Title{id}", $"Artist{id}", id);
    }

    private static Playlist Create(int capacity, params int[] durations)
    {
        var playlist = new Playlist(capacity);
        foreach (var duration in durations)
            playlist.Add(MakeTrack(duration));

        return playlist;
    }

    // Проверяем форматирование списка, а не повторяем проверку Track.ToString.
    private static string Lines(params int[] durations)
    {
        return string.Join(Environment.NewLine,
            Array.ConvertAll(durations, duration => MakeTrack(duration).ToString()));
    }

    [Test]
    [Description("Конструктор: capacity < 0 -> исключение. Вторая ветвь покрыта созданием плейлистов в остальных тестах.")]
    public void Constructor_NegativeCapacity_Throws()
    {
        Action act = () => new Playlist(-1);
        act.Should().ThrowExactly<PlaylistException>();
    }

    [Test]
    [Description("Add: есть место -> трек уникален -> запись. Заодно TotalDuration и обе ветви i > 0 в ToString.")]
    public void Add_UniqueTracks_UpdatesCountDurationAndText()
    {
        var playlist = Create(2, 1, 2);

        using (new AssertionScope())
        {
            playlist.Count.Should().Be(2);
            playlist.Capacity.Should().Be(2);
            playlist.TotalDuration().Should().Be(3);
            playlist.ToString().Should().Be(Lines(1, 2));
        }
    }

    [Test]
    [Description("Add: Count >= Capacity -> исключение до проверки дубликата.")]
    public void Add_FullPlaylist_Throws()
    {
        var playlist = Create(1, 1);

        var act = () => playlist.Add(MakeTrack(2));
        act.Should().ThrowExactly<PlaylistException>();
        playlist.Count.Should().Be(1);
    }

    [Test]
    [Description("Add: место есть -> UniqueTracks.Add возвращает false -> исключение.")]
    public void Add_DuplicateWithAvailableSpace_Throws()
    {
        var playlist = Create(2, 1);

        var act = () => playlist.Add(MakeTrack(1));
        act.Should().ThrowExactly<PlaylistException>();
        playlist.Count.Should().Be(1);
    }

    [TestCase(-1, false, TestName = "Getter_01_ПервоеУсловиеЛожно")]
    [TestCase(3, false, TestName = "Getter_02_ВтороеУсловиеЛожно")]
    [TestCase(0, true, TestName = "Getter_03_ОбаУсловияИстинны")]
    public void IndexerGet_CurrentConditions(int index, bool returnsTrack)
    {
        var playlist = Create(3, 1, 2, 3);

        if (returnsTrack)
            playlist[index].Duration.Should().Be(1);
        else
        {
            var act = () => { _ = playlist[index]; };
            act.Should().ThrowExactly<PlaylistException>();
        }
    }

    [TestCase(-1, false, TestName = "Setter_01_ПервоеУсловиеИстинно_Исключение")]
    [TestCase(3, false, TestName = "Setter_02_ВтороеУсловиеИстинно_Исключение")]
    [TestCase(1, true, TestName = "Setter_03_ОбаУсловияЛожны_Запись")]
    public void IndexerSet_ValidAndInvalidIndex(int index, bool writesTrack)
    {
        var playlist = Create(3, 1, 2, 3);
        var replacement = MakeTrack(4);

        if (writesTrack)
            playlist[index] = replacement;
        else
        {
            Action act = () => playlist[index] = replacement;
            act.Should().ThrowExactly<PlaylistException>();
        }

        playlist.ToString().Should().Be(writesTrack ? Lines(1, 4, 3) : Lines(1, 2, 3));
    }

    [TestCase(0, false, TestName = "Operators_01_ОднаСсылка")]
    [TestCase(1, true, TestName = "Operators_02_LeftNull")]
    [TestCase(2, true, TestName = "Operators_03_RightNull")]
    [TestCase(3, true, TestName = "Operators_04_РазноеКоличество")]
    [TestCase(4, true, TestName = "Operators_05_РазличиеВторогоТрека")]
    [TestCase(5, false, TestName = "Operators_06_РавныеПлейлисты")]
    public void Operators_CurrentReachableBranches(int scenario, bool expectedNotEqual)
    {
        var left = scenario == 1 ? null : Create(2, 1, 2);
        var right = scenario switch
        {
            0 => left,
            2 => null,
            3 => Create(2, 1),
            4 => Create(2, 1, 3),
            _ => Create(2, 1, 2)
        };

        // != сам вызывает ==: одним вызовом выполняются оба оператора.
        (left != right).Should().Be(expectedNotEqual);
    }

    [Test]
    [Description("Merge: исходный набор -> пропуск дубликата и сбор новых -> места хватает -> добавление в порядке other.")]
    public void Merge_DuplicateAndNewTracks_AddsOnlyNewTracks()
    {
        var playlist = Create(4, 1, 2);
        var other = Create(3, 2, 3, 4);

        playlist.Merge(other);

        using (new AssertionScope())
        {
            playlist.Count.Should().Be(4);
            playlist.ToString().Should().Be(Lines(1, 2, 3, 4));
        }
    }

    [Test]
    [Description("Merge: сбор новых -> места не хватает -> исключение до изменения массива.")]
    public void Merge_NotEnoughCapacity_ThrowsWithoutChangingPlaylist()
    {
        var playlist = Create(3, 1, 2);
        var other = Create(2, 3, 4);

        var act = () => playlist.Merge(other);
        act.Should().ThrowExactly<PlaylistException>();
        using (new AssertionScope())
        {
            playlist.Count.Should().Be(2);
            playlist.ToString().Should().Be(Lines(1, 2));
        }
    }

    [Test]
    [Description("RemoveTracksOf: встречаются и удаляемые, и оставляемые треки -> сдвиг -> очистка хвоста -> новый Count.")]
    public void RemoveTracksOf_MixedTracks_RemovesMatchesAndPreservesOrder()
    {
        var playlist = Create(4, 1, 2, 3, 4);
        var other = Create(2, 2, 4);

        playlist.RemoveTracksOf(other);

        using (new AssertionScope())
        {
            playlist.Count.Should().Be(2);
            playlist.ToString().Should().Be(Lines(1, 3));
        }
    }

    [Test]
    [Description("RemoveTracksOf: совпадений нет -> write == Count -> исключение.")]
    public void RemoveTracksOf_NoMatches_Throws()
    {
        var playlist = Create(2, 1, 2);
        var other = Create(1, 3);

        var act = () => playlist.RemoveTracksOf(other);
        act.Should().ThrowExactly<PlaylistException>();
        playlist.Count.Should().Be(2);
    }

    [TestCase("Artist2", true, TestName = "FindByArtist_01_ПропускПервого_ВозвратВторого")]
    [TestCase("Missing", false, TestName = "FindByArtist_02_НетСовпадений")]
    public void FindByArtist_FoundAndNotFound(string artist, bool found)
    {
        var playlist = Create(2, 1, 2);

        if (found)
            playlist.FindByArtist(artist).Should().Be(1);
        else
        {
            Action act = () => playlist.FindByArtist(artist);
            act.Should().ThrowExactly<PlaylistException>();
        }
    }
}
