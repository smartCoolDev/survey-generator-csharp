﻿using System;
using FakeSurveyGenerator.Domain.AggregatesModel.SurveyAggregate;

namespace FakeSurveyGenerator.Domain.Services
{
    public class OneSidedVoteDistributionStrategy : IVoteDistributionStrategy
    {
        public void DistributeVotes(Survey survey)
        {
            var random = new Random();

            var winningOptionIndex = random.Next(0, survey.Options.Count);

            for (var i = 0; i < survey.NumberOfRespondents; i++)
            {
                survey.Options[winningOptionIndex].AddVote();
            }
        }
    }
}
