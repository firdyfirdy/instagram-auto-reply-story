using InstaAutoReplyStory.Controller;
using InstaAutoReplyStory.Helpers;
using InstaAutoReplyStory.Model;
using InstagramApiSharp;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using System;
using System.Threading.Tasks;

namespace InstaAutoReplyStory
{
  class Program
  {
    private static bool isLogin = false;
    public static Random random = new Random();
    public static long unixTime = HelpersApi.UnixTimeNow();
    static async Task Main(string[] args)
    {
      var consoleRed = ConsoleColor.Red;
      var consoleGreen = ConsoleColor.Green;

      HelpersApi.WriteLine("Instagram Auto Follows, Comments, and Likes!", consoleGreen);
      HelpersApi.WriteLine("Contact me: me@firdy.dev", consoleGreen);
      Console.WriteLine();

      Console.Write("Username: ");
      string username = Console.ReadLine();
      Console.Write("Password: ");
      string password = Console.ReadLine();

      Console.Write("Target Username: ");
      string target = Console.ReadLine();
      Console.Write("Comments ( Delimeter with semicolon \";\" ): ");
      string captions = Console.ReadLine();
      Console.Write("Delay (in miliseconds): ");
      if (!int.TryParse(Console.ReadLine(), out int delay))
      {
        HelpersApi.WriteLine($"Error, Not valid arguments.", consoleRed);
        return;
      }

      /* Set Instagram Session */
      UserSessionData sessionData = new UserSessionData()
      {
        UserName = username,
        Password = password
      };

      Actions instaActions = new Actions(sessionData);
      ActionModel login = await instaActions.DoLogin();

      HelpersApi.WriteLine("Trying to login ...", consoleGreen);

      /* Login */
      HelpersApi.WriteLine(login.Response);

      /* Login Success */
      if (login.Status == 1)
      {
        isLogin = true;
      }

      /* Login Challange */
      if (login.Status == 2)
      {
        await instaActions.SendCode();
        HelpersApi.WriteLine("Put your code: ");
        string code = Console.ReadLine();

        ActionModel verifyCode = await instaActions.VerifyCode(code);
        HelpersApi.WriteLine(verifyCode.Response);
        if (verifyCode.Status == 1)
          isLogin = true;
      }

      if (isLogin)
      {
        // Follow my instagram accounts (firdyfirdy)
        await instaActions.DoFollow(5600985630);

        string LatestMaxId = "";
        int i = 0, countTotalStory = 0;
        bool doReply = false;

        /* Get Target Informations */
        IResult<InstaUserInfo> targetInfo = await HelpersApi.InstaApi.UserProcessor
          .GetUserInfoByUsernameAsync(target);
        if (targetInfo.Succeeded)
        {
          HelpersApi.WriteLine($"[+] Target Username: {targetInfo.Value.Username} " +
            $"| Followers: {targetInfo.Value.FollowerCount}");
          Console.WriteLine();

          while (LatestMaxId != null)
          {
            /** Get followers list**/
            var targetFollowers = await HelpersApi.InstaApi.UserProcessor
                .GetUserFollowersAsync(target, PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(LatestMaxId));
            if (targetFollowers.Succeeded)
            {
              LatestMaxId = targetFollowers.Value.NextMaxId;
              foreach (var targetFolls in targetFollowers.Value)
              {
                if (!targetFolls.IsPrivate)
                {
                  /* Get Friendship status */
                  var getFriendshipStatus = await HelpersApi.InstaApi.UserProcessor.GetFriendshipStatusAsync(targetFolls.Pk);
                  if (getFriendshipStatus.Succeeded)
                  {
                    /* if they follow us */
                    if (getFriendshipStatus.Value.Following)
                    {
                      HelpersApi.WriteLine($"[{i}] Username: {targetFolls.UserName} | Skipped, Already Follow You.", consoleRed);
                    }
                    else if (getFriendshipStatus.Value.FollowedBy)
                    {
                      HelpersApi.WriteLine($"[{i}] Username: {targetFolls.UserName} | Skipped, You Already Follow.", consoleRed);
                    }
                    else
                    {
                      var getVictimStory = await HelpersApi.InstaApi.StoryProcessor.GetUserStoryAsync(targetFolls.Pk);
                      if (getVictimStory.Succeeded)
                      {
                        HelpersApi.WriteLine($"[{i}] Username: {targetFolls?.UserName} | Story: {getVictimStory?.Value?.Items?.Count}", ConsoleColor.Green);
                        var totalStory = getVictimStory.Value.Items.Count;
                        if (totalStory >= 1)
                        {
                          if (getVictimStory.Value.CanReply)
                          {
                            // Follow target
                            await instaActions.DoFollow(targetFolls.Pk);
                            string resultCaptions = "";
                            doReply = false;

                            foreach (var victimStory in getVictimStory.Value.Items)
                            {
                              /* Split Captions */
                              string[] captionSplit = captions.Split(';');
                              int rnd = random.Next(0, captionSplit.Length);
                              resultCaptions = captionSplit[rnd].Trim();

                              /* Mark story as seen */
                              var seen = await instaActions.DoSeeStory(victimStory.Id, unixTime);

                              /* Story Sliders */
                              foreach (var victimStorySlider in victimStory.StorySliders)
                              {
                                var slider = await instaActions.DoSlideStory(victimStory.Id, victimStorySlider.SliderSticker.SliderId);

                                if (slider.Status == 1)
                                {
                                  HelpersApi.WriteLine(slider.Response, ConsoleColor.Green);
                                }
                                else
                                {
                                  HelpersApi.WriteLine(slider.Response, ConsoleColor.Red);
                                }
                              }

                              /* Story Votes */
                              foreach (var victimStoryVotes in victimStory.StoryPollVoters)
                              {
                                var vote = await instaActions.DoVoteStory(victimStory.Id, victimStoryVotes.PollId);
                                if (vote.Status == 1)
                                {
                                  HelpersApi.WriteLine(vote.Response, ConsoleColor.Red);
                                }
                                else
                                {
                                  HelpersApi.WriteLine(vote.Response, ConsoleColor.Red);
                                }
                              }

                              /* Story Questions */
                              foreach (var victimStoryQuestions in victimStory.StoryQuestions)
                              {
                                var answerStory = await instaActions.DoReplyQuestionStory(victimStory.Id, victimStoryQuestions.QuestionSticker.QuestionId, resultCaptions);

                                if (answerStory.Status == 1)
                                {
                                  HelpersApi.WriteLine(answerStory.Response, ConsoleColor.Green);
                                }
                                else
                                {
                                  HelpersApi.WriteLine(answerStory.Response, ConsoleColor.Red);
                                }
                              }

                            }

                            /* Reply last story */
                            var replyStory = await instaActions.DoReplyStory(getVictimStory.Value.Items[totalStory - 1].Id, targetFolls.Pk, resultCaptions);
                            if (replyStory.Status == 1)
                            {
                              doReply = true;
                              HelpersApi.WriteLine(replyStory.Response, consoleGreen);
                            }
                            else
                            {
                              HelpersApi.WriteLine(replyStory.Response, consoleRed);
                            }

                            if (doReply)
                            {
                              HelpersApi.WriteLine($"[+] Sleep for {delay} ms");
                              await Task.Delay(delay);
                            }
                          }
                          else
                          {
                            HelpersApi.WriteLine($"[+] Username: {targetFolls.UserName} | Error: Reply disabled.", consoleRed);
                          }
                        }
                        
                      }
                      else
                      {
                        HelpersApi.WriteLine($"[{i}] Username: {targetFolls.UserName} | {getVictimStory.Info.Message}", consoleRed);
                      }
                    }
                  }
                  else
                  {
                    HelpersApi.WriteLine($"[{i}] Username: {targetFolls.UserName} | Error: {getFriendshipStatus.Info.Message}", consoleRed);
                  }
                }
                else
                {
                  HelpersApi.WriteLine($"[{i}] Username: {targetFolls.UserName} | Error: Account is private.", consoleRed);
                }
                i++;
              }
            }
            else
            {
              HelpersApi.WriteLine($"[+] Get Target Followers | Error: {targetFollowers.Info.Message}", consoleRed);
            }
          }
        }
        else
        {
          HelpersApi.WriteLine($"[+] Get Target Info | Error: {targetInfo.Info.Message}");
        }
      }
    }
  }
}
