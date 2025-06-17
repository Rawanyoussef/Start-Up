using Microsoft.Extensions.Logging;
using StartUP.Data.Entity;
using StartUP.Repository.ProjectRepo;
using StartUP.Service.Dtos.Prediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StartUP.Service.PredictionService
{
    public class PredictionService : IPredictionService
    {
        private readonly IProjecRepo _projectRepo;
        private readonly HttpClient _httpClient;

        public PredictionService(IProjecRepo projectRepo,
                                 IHttpClientFactory httpClientFactory)
        {
            _projectRepo = projectRepo;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<object?> PredictProjectStatusAsync(PredictionStatusDto predictionStatusDto , int projectId)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) return null;
          
            var totalInvestForProject = await _projectRepo.GetTotalInvestmentForProjectAsync(projectId);
            var totalFundingRecieved =project.TotalFundingRecieved +predictionStatusDto.TotalFundingRecieved + totalInvestForProject;
            var dto = new ProjectStatusDto
            {
                TotalFundingRecieved = totalFundingRecieved, /// + investamount from form + total invest for project
                FundingAmount = project.FundingAmount,
                TotalFundingRounds = project.TotalFundingRounds,
                TotalMilestones = project.TotalMilestones,
                TotalPartenerships = project.TotalPartenerships,
                NoOfInvestors = project.NoOfInvestors,
                FoundingYear = project.FoundingYear,
                FundingYear = predictionStatusDto.FundingYear, /// from form
                MileStoneYear = project.MileStoneYear,
                AverageFundingPerRound = project.AverageFundingPerRound,
                TimeToFirstFunding = Math.Round((project.FirstFundedAt - new DateTime(project.FoundingYear, 1, 1)).TotalDays / 365.0, 2),
                CategoryEncoder = (int)project.Category,
                IsActiveTillYear = project.IsActiveTill.Year
            };

            return await SendToFlask("project_status", dto);
        }

        public async Task<object?> PredictFundingRoundTypeAsync(int projectId)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) return null;

            var dto = new FundingRoundTypeDto
            {
                TotalFundingRecieved = project.TotalFundingRecieved,
                TotalFundingRounds = project.TotalFundingRounds,
                TotalPartenerships = project.TotalPartenerships,
                NoOfInvestors = project.NoOfInvestors,
                FundingAmount = project.FundingAmount,
                FoundingYear = project.FoundingYear,
                FundingYear = project.FundingYear,
                CompanyAge = project.CompanyAge,
                AverageFundingPerRound = project.AverageFundingPerRound,
                TimeToFirstFunding = Math.Round((project.FirstFundedAt - new DateTime(project.FoundingYear, 1, 1)).TotalDays / 365.0, 2),
                CategoryEncoder = (int)project.Category,
                StatusEncoder = MapStatusToCode(project.Status)
            };

            return await SendToFlask("funding_round_type", dto);
        }

        public async Task<object?> PredictTotalFundingRecievedAsync(int projectId)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) return null;

            var dto = new TotalFundingRecievedDto
            {
                TotalFundingRounds = project.TotalFundingRounds,
                TotalMilestones = project.TotalMilestones,
                TotalPartenerships = project.TotalPartenerships,
                NoOfInvestors = project.NoOfInvestors,
                FundingAmount = project.FundingAmount,
                FundAmountRaised = project.FundAmountRaised,
                FoundingYear = project.FoundingYear,
                FundingYear = project.FundingYear,
                FundingFundYear = project.FundingFundYear,
                AverageFundingPerRound = project.AverageFundingPerRound,
                TimeToFirstFunding = Math.Round((project.FirstFundedAt - new DateTime(project.FoundingYear, 1, 1)).TotalDays / 365.0, 2),
                CategoryEncoder = (int)project.Category,
                StatusEncoder = MapStatusToCode(project.Status),
                FundingRoundTypeEncoder = MapFundingRoundTypeToCode(project.FundingRoundType)
            };

            return await SendToFlask("total_funding_recieved", dto);
        }

        public async Task<object?> PredictFundingAmountAsync(int projectId)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) return null;

            var dto = new FundingAmountDto
            {
                TotalFundingRecieved = project.TotalFundingRecieved,
                TotalFundingRounds = project.TotalFundingRounds,
                TotalMilestones = project.TotalMilestones,
                FoundingYear = project.FoundingYear,
                FundingYear = project.FundingYear,
                CompanyAge = project.CompanyAge,
                AverageFundingPerRound = project.AverageFundingPerRound,
                TimeToFirstFunding = Math.Round((project.FirstFundedAt - new DateTime(project.FoundingYear, 1, 1)).TotalDays / 365.0, 2),
                CategoryEncoder = (int)project.Category,
                StatusEncoder = MapStatusToCode(project.Status),
                CountryEncoder = MapCountryToCode(project.Country),
                FundingRoundTypeEncoder = MapFundingRoundTypeToCode(project.FundingRoundType)
            };

            return await SendToFlask("funding_amount", dto);
        }

        private async Task<JsonDocument> SendToFlask(string modelName, object dto)
        {
            var url = $"http://192.168.1.3:5000/predict/{modelName}";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, dto);
                var content = await response.Content.ReadAsStringAsync();

                var json = JsonDocument.Parse(content);

                if (!response.IsSuccessStatusCode)
                {
                    // حاول تقرأ رسالة الخطأ من JSON لو موجودة
                    if (json.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        string errorMessage = errorProp.GetString() ?? "Unknown error";

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            errorMessage = $"Model '{modelName}' not found at endpoint '{url}': {errorMessage}";
                        }

                        throw new Exception($"Flask API Error {response.StatusCode}: {errorMessage}");
                    }

                    // مفيش "error" في الـbody، نرمي الرسالة كاملة
                    throw new Exception($"Flask API Error {response.StatusCode}: {content}");
                }

                // Success - رجّع الـ JSON
                return json;
            }
            catch (JsonException)
            {
                throw new Exception("Failed to parse JSON from Flask API response.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception while calling Flask API: {ex.Message}", ex);
            }
        }


        public async Task<int> SaveFundingDetailsAsync(int projectId, decimal totalFunding, string roundType, decimal fundingAmount)
        {
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null) throw new Exception("Project Not Found");

            try
            {
                var daysLeft = (project.IsActiveTill - DateTime.UtcNow).Days;

                if (project.FundingDetails == null)
                {
                    project.FundingDetails = new FundingDetails();
                }

                project.FundingDetails.TotalInvestment = totalFunding;
                project.FundingDetails.NextRoundType = roundType;
                project.FundingDetails.NextRoundFunding = fundingAmount;
                project.FundingDetails.DaysLeft = daysLeft;

                await _projectRepo.SaveChangesAsync();
                return project.UserId;
            }
            catch
            {
                return -1; // indicates failure
            }
        }


        public async Task<PredictionResponseDto> GetPredictionAsync(int projectId)
        {
            var prediction = await _projectRepo.GetFundingDetailsAsync(projectId);
            if (prediction == null) throw new Exception($"No FundingDetails for this project {projectId}");
            return new PredictionResponseDto
            {
                NextRoundFunding= prediction.NextRoundFunding,
                NextRoundType= prediction.NextRoundType,
                TotalInvestment = prediction.TotalInvestment,
            };
        }


        private int MapCountryToCode(string? country) => country?.ToLower() switch
        {
            "egypt" => 26,
            "usa" => 1,
            _ => 0
        };

        private int MapStatusToCode(string? status) => status?.ToLower() switch
        {
            "closed" => 1,
            "ipo" => 2,
            "operating" => 3,
            "acquired" => 0
        };

        private int MapFundingRoundTypeToCode(string? type) => type?.ToLower() switch
        {
            "angel" => 0,
            "crowdfunding" => 1,
            "other" => 2,
            "post-ipo" => 3,
            "private-equity" => 4,
            "series-a" => 5,
            "series b" => 6,
            "series-c+" => 7,
            "venture" => 8,
            _ => -1
        };

    }
}
