using System;
using System.Collections.Generic;
using System.Linq;

namespace MLLib.AI.GA
{
    public class Population
    {
        public int Count;
        public List<Genome> Pop;

        public const int CrossoverRange = 5;

        public Population(IEnumerable<Genome> pop)
        {
            Pop = new List<Genome>(pop);
            for (var i = 0; i < Pop.Count; i++)
                Pop[i].CreatureID = i;
            Count = Pop.Count;
        }

        public Population(int count, Func<int, Genome> creatorFunc)
        {
            Pop = new List<Genome>();
            for (var i = 0; i < count; i++)
            {
                var gen = creatorFunc(i);
                gen.CreatureID = i;
                Pop.Add(gen);
            }

            Count = count;
        }

        public Genome BestCreature(bool min)
        {
            foreach (var genome in Pop)
                genome.CalculateFitness();

            return GetSortedPop(min)[0];
        }

        public void Selection(bool minimize, int take = -1)
        {
            foreach (var genome in Pop)
                genome.CalculateFitness();

            Pop = GetSortedPop(minimize).Take(take == -1 ? Count / 2 : take).ToList();
        }

        private List<Genome> GetSortedPop(bool min)
        {
            return min ? Pop.OrderBy(p => p.Fitness).ToList() : Pop.OrderByDescending(p => p.Fitness).ToList();
        }

        private int ClipCrossoverIndex(int i, bool min)
        {
            if (min)
                return i < 0 ? 0 : i;
            else
                return i > Pop.Count - 1 ? Pop.Count - 1: i;
        }

        public void Crossover(CrossoverAlgorithm algorithm, bool gaussian = true)
        {
            var newPop = new List<Genome>();
            for (var i = 0; i < Count / 2; i += 2)
            {
                var minIndex = ClipCrossoverIndex(-CrossoverRange, true);
                var maxIndex = ClipCrossoverIndex(CrossoverRange, false);

                var i1 = (int)Genome.Random(gaussian, minIndex, maxIndex);
                var i2 = (int)Genome.Random(gaussian, minIndex, maxIndex);

                if (i1 == i2)
                {
                    i1 = ClipCrossoverIndex(i1 - 1, true);
                    i2 = ClipCrossoverIndex(i2 + 1, false);
                }

                newPop.AddRange(Genome.Crossover(Pop[i1], Pop[i2], algorithm, gaussian));
            }
            Pop.AddRange(newPop);
        }

        public void Mutate(double mutationRate, bool gaussian = false)
        {
            foreach (var genome in Pop)
                genome.Mutate(mutationRate, gaussian);
        }

        public List<object> GetStates()
        {
            foreach (var genome in Pop)
                genome.CalculateFitness();

            return Pop.Select(p => p.State).ToList();
        }

        public double AverageFitness()
        {
            return Pop.Sum(p => p.Fitness) / Pop.Count;
        }
    }
}