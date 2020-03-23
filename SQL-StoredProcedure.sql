-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Cody Mathis
-- Create date: 2020-03-23
-- Description:	Return a list of machines from the Client Health DB for the specified username
-- =============================================
CREATE PROCEDURE GetEndpointsForUser
    @UserName nvarchar(50) 
AS   
	DECLARE @UserNameFilter nvarchar(50) = '%' + @UserName
    SET NOCOUNT ON;  
	SELECT Hostname
		, OperatingSystem
		, LastLoggedOnUser
	FROM [dbo].Clients WHERE LastLoggedOnUser LIKE @UserNameFilter
GO  
