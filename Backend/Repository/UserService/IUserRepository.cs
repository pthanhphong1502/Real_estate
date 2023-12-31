﻿using Backend.Repository.UserService.Dtos;
using Backend.Helper;

namespace Backend.Repository.UserService
{
    public interface IUserRepository
    {
        public Task LockUserAsync(string id, LockUserDto model);
        public Task ChangePasswordAsync(string id, ChangePasswordDto model);
        public Task<List<GetAllUserDto>> GetAllUserAsync(int page, int pageSize);
        public Task<GetDetailUserDto> GetUserAsync(string id);
        public Task<List<GetAllUserDto>> SearchUser (SearchUserDto searchUserDto); 
        public Task<UpdateUserDto> UpdateUserAsync(string userId, UpdateUserDto model);
        public Task<GetDetailUserDto> GetCurrentUserAsync(string id);
    }
}
