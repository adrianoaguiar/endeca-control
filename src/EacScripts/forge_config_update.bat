@echo off
REM
REM Created by Bane Debeljevic 2014
REM

call %~dp0..\config\script\set_environment.bat
call %~dp0runcommand.bat ForgeConfigUpdate run 2>&1