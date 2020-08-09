﻿using AphrieTask.BE;
using Entities;
using Entities.Enums;
using Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AphrieTask.Manager
{
    public class PostManager
    {
        private IBaseRepository<Friend> _friendRepository;
        private IBaseRepository<Post> _postRepository;
        private IBaseRepository<PostInteraction> _postInteractionRepository;
        private IBaseRepository<LoacalizProperty> _loacalizPropertyRepository;

        public PostManager(IBaseRepository<Friend> friendRepository,
            IBaseRepository<Post> postRepository,
            IBaseRepository<LoacalizProperty> loacalizPropertyRepository,
            IBaseRepository<PostInteraction> postInteractionRepository)
        {
            _friendRepository = friendRepository;
            _postRepository = postRepository;
            _loacalizPropertyRepository = loacalizPropertyRepository;
            _postInteractionRepository = postInteractionRepository;
        }

        public List<PostLocalizeInfo> GetMyPosts(Guid profileID,string language)
        {
            List<PostLocalizeInfo> postLocalizeInfoList = new List<PostLocalizeInfo>();
            try
            {
                var posts = _postRepository.Where(p => p.profileId == profileID && !p.isDeleted).Result.ToList();

                foreach (var post in posts)
                {
                    var LocalizeInfo = _loacalizPropertyRepository.Where(p => p.localizeEntityId == post.Id&& p.localizeLanguge.languageName.ToLower() == language.ToLower()).Result.ToList();

                    PostLocalizeInfo postLocalizeInfo = new PostLocalizeInfo();
                    postLocalizeInfo.postInfo = post;
                    postLocalizeInfo.localizeInfo = LocalizeInfo;

                    postLocalizeInfoList.Add(postLocalizeInfo);
                }

                return postLocalizeInfoList;
            }
            catch
            {
                return null;
            }
        }

        public bool AddNewPost(PostLocalizeInfo post)
        {
            try
            {
                _postRepository.Insert(post.postInfo);
                _loacalizPropertyRepository.InsertRange(post.localizeInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddNewPostInteraction(PostInteraction postInteraction)
        {
            try
            {
                _postInteractionRepository.Insert(postInteraction);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<PostLocalizeInfo> GetPostsCanSee(Guid profileID,string language)
        {
            var friendsIdsWhoICanSee = GetMyCurrentlyFriendsAsync(profileID).Result;

            List<PostLocalizeInfo> postLocalizeInfoList = new List<PostLocalizeInfo>();
            try
            {
                var posts = _postRepository.Where(p => friendsIdsWhoICanSee.Contains(p.profileId) && !p.isDeleted).Result.ToList();

                foreach (var post in posts)
                {
                    var LocalizeInfo = _loacalizPropertyRepository.Where(p => p.localizeEntityId == post.Id&& p.localizeLanguge.languageName.ToLower()==language.ToLower()).Result.ToList();

                    PostLocalizeInfo postLocalizeInfo = new PostLocalizeInfo();
                    postLocalizeInfo.postInfo = post;
                    postLocalizeInfo.localizeInfo = LocalizeInfo;

                    postLocalizeInfoList.Add(postLocalizeInfo);
                }

                return postLocalizeInfoList;
            }
            catch
            {
                return null;
            }

        }

        private async Task<List<Guid>> GetMyCurrentlyFriendsAsync(Guid? clientId)
        {

            var navfriendProperties = new string[] { "ReceiverProfile" };
            var navsenderProperties = new string[] { "SenderProfile" };
            var mymadefriends = await _friendRepository.Where(x => x.SenderProfile.Id == clientId.Value &&
            x.RelationStatus == RelationStatus.Accepted, navfriendProperties);
            var myivnitelist = await _friendRepository.Where(x => x.ReceiverProfile.Id == clientId.Value &&
            x.RelationStatus == RelationStatus.Accepted, navsenderProperties);
            List<Friend> rslt = new List<Friend>();
            rslt.AddRange(mymadefriends);
            rslt.AddRange(myivnitelist);


            List<Guid> profilesID = new List<Guid>();
            foreach (var item in rslt)
            {
                if (item.SenderProfileId == clientId)
                {
                    profilesID.Add(item.receiverProfileId);
                }
                else if (item.receiverProfileId == clientId)
                {
                    profilesID.Add(item.SenderProfileId);
                }
            }
            return profilesID;


        }

        internal void RemovePost(Guid id)
        {
            var post = _postRepository.GetById(id).Result;
            post.isDeleted = true;
            _postRepository.Update(post);

            var LocalizeInfo = _loacalizPropertyRepository.Where(p => p.localizeEntityId == post.Id).Result.ToList();

            foreach (var item in LocalizeInfo)
            {
                item.isDeleted = true;
                _loacalizPropertyRepository.Update(item);
            }

        }


    }
}