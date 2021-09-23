using System.Collections.Generic;

using Codice.CM.Common;
using Codice.Tool;

namespace Codice.Views.Changesets
{
    static class LaunchDiffOperations
    {
        internal static void DiffChangeset(
            RepositorySpec repSpec,
            ChangesetExtendedInfo changesetExtendedInfo,
            bool isGluonMode)
        {
            if (changesetExtendedInfo == null)
                return;

            string changesetFullSpec = GetChangesetFullSpec(
                repSpec, changesetExtendedInfo);

            LaunchTool.OpenChangesetDiffs(
                changesetFullSpec,
                isGluonMode);
        }

        internal static void DiffSelectedChangesets(
            RepositorySpec repSpec,
            ChangesetExtendedInfo cset1,
            ChangesetExtendedInfo cset2,
            bool isGluonMode)
        {
            ChangesetExtendedInfo srcChangesetExtendedInfo;
            ChangesetExtendedInfo dstChangesetExtendedInfo;

            GetSrcAndDstCangesets(
                cset1,
                cset2,
                out srcChangesetExtendedInfo,
                out dstChangesetExtendedInfo);

            string srcChangesetFullSpec = GetChangesetFullSpec(
                repSpec,
                srcChangesetExtendedInfo);

            string dstChangesetFullSpec = GetChangesetFullSpec(
                repSpec,
                dstChangesetExtendedInfo);

            LaunchTool.OpenSelectedChangesetsDiffs(
                srcChangesetFullSpec,
                dstChangesetFullSpec,
                isGluonMode);
        }

        internal static void DiffBranch(
            RepositorySpec repSpec,
            ChangesetExtendedInfo changesetExtendedInfo,
            bool isGluonMode)
        {
            if (changesetExtendedInfo == null)
                return;

            string branchFullSpec = GetBranchFullSpec(
                repSpec,
                changesetExtendedInfo);

            LaunchTool.OpenBranchDiffs(
                branchFullSpec,
                isGluonMode);
        }

        static void GetSrcAndDstCangesets(
            ChangesetExtendedInfo cset1,
            ChangesetExtendedInfo cset2,
            out ChangesetExtendedInfo srcChangesetExtendedInfo,
            out ChangesetExtendedInfo dstChangesetExtendedInfo)
        {
            if (cset1.LocalTimeStamp < cset2.LocalTimeStamp)
            {
                srcChangesetExtendedInfo = cset1;
                dstChangesetExtendedInfo = cset2;
                return;
            }

            srcChangesetExtendedInfo = cset2;
            dstChangesetExtendedInfo = cset1;

        }

        static string GetChangesetFullSpec(
            RepositorySpec repSpec,
            ChangesetExtendedInfo changesetExtendedInfo)
        {
            return string.Format("cs:{0}@{1}",
                changesetExtendedInfo.ChangesetId,
                repSpec.ToString());
        }

        static string GetBranchFullSpec(
            RepositorySpec repSpec,
            ChangesetExtendedInfo changesetExtendedInfo)
        {
            return string.Format("br:{0}@{1}",
                changesetExtendedInfo.BranchName,
                repSpec.ToString());
        }
    }
}
