<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SocialEventPlannerWeb.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Social Events</title>
    <link href="../ep.css" rel="stylesheet" />
    <script src="../Scripts/jquery-1.11.1.min.js"></script>
    <script src="../Scripts/jquery-ui-1.10.4/js/jquery-ui-1.10.4.js"></script>
    <link href="../Scripts/jquery-ui-1.10.4/css/ui-lightness/jquery-ui-1.10.4.css" rel="stylesheet" />
    <script src="../Scripts/jquery.blockUI.js"></script>
</head>
<body>

    <script type="text/javascript">

        $(document).ready(function () {

            //hide the new event div
            $("#newEventDiv").hide();

            //load up the list of events when the page loads
            getEvents();

            //hook up the date picker
            $(".eventDateTxt").each(function () {
                $(this).datepicker();
            });
        });


        function showWait() {
            $("div.eventsMainDiv").block({ message: '<h3><img src="../images/busy.gif" style="margin-right:20px;" />Please wait while we work on your request...</h3>' }); 
        }

        function hideWait() {
            $("div.eventsMainDiv").unblock();
        }

        function getEvents() {

            showWait();

            //get the hiddens
            var hostUrl = $("#hdnHostWeb").val();
            var accessToken = $("#hdnAccessToken").val();

            //create the JSON string to post
            var formData = "{hostUrl:" + hostUrl + "," +
                "accessToken:" + accessToken + "}";

            //make it POST ready
            formData = JSON.stringify(formData);

            $.post("/api/events/currentevents", { '': formData })
            .success(function (data)
            {
                if (data.length == 0) {

                    var noData = "<span class=\"noData\">There aren't any events right now.  You can " +
                        "use the link below to create a new event.</span>";
                    $("#eventsDataDiv").html(noData);
                }
                else {
                    var info = "<span class=\"eventTitle\">Your Events:</span><br />" + 
                        "<table class=\"eventTbl\"><tr><td></td><td class=\"eventCellTitle\">Event</td><td class=\"eventCellTitle\">Date</td><td class=\"eventCellTitle\">Twitter Tags</td></tr>";

                    //enumerate each match
                    for (var i = 0; i < data.length; i++) {
                        info += "<tr><td><img class=\"eventActionLink\" style=\"cursor:pointer;margin-right:15px;\" src=\"../images/edit.png\" title=\"Update the Twitter tags for this event\" onclick=\"editItem('" +
                            data[i].objectGraphID + "');\" />" + 
                            "<img class=\"eventActionLink\" style=\"cursor:pointer;margin-right:15px;\" src=\"../images/delete.gif\" title=\"Delete the event\" onclick=\"deleteItem('" +
                            data[i].objectGraphID + "');\" /></td>" + 
                            "<td class=\"eventCellData\"><a href=\"" +
                            data[i].newSiteUrl + "\" target=\"_blank\" class=\"eventSiteLink\">" +
                            data[i].eventName + "</td><td class=\"eventCellData\">" +
                            data[i].eventDate + "</td><td class=\"eventCellData\">" +
                            "<input type=\"text\" id=\"" + data[i].objectGraphID +
                            "\" value=\"" + data[i].twitterTags + "\"/></td></tr>";
                    }

                    //close off the table code
                    info += "</table>";

                    $("#eventsDataDiv").html(info);

                    //$("div.eventsMainDiv").unblock();
                    hideWait();
                }
            }).fail(function (errMsg) {
                hideWait();
                alert("Sorry, there was a problem and we couldn't get your events: " + errMsg.responseText);
            });

        }

        function showNewEvent() {

            //show the new event div
            $("#newEventDiv").show(750);
        }

        function hideNewEvent() {
            //hide the new event div
            $("#newEventDiv").hide(750);
        }

        function addNewEvent() {

            //validate data first
            var event = $("#eventNameTxt").val();
            var eventDate = $("#eventDateTxt").val();
            var tags = $("#twitterTagsTxt").val();

            if ((event == undefined) || (event == "")) {
                alert("You must provide an Event name.");
                return;
            }

            if ((eventDate == undefined) || (eventDate == "")) {
                alert("You must provide an Event date.");
                return;
            }

            if ((tags == undefined) || (tags == "")) {
                alert("You must provide a semi-colon delimited list of Twitter tags to follow.");
                return;
            }

            hideNewEvent();
            showWait();

            //get the hiddens
            var hostUrl = $("#hdnHostWeb").val();
            var accessToken = $("#hdnAccessToken").val();

            //create the JSON string to post
            var formData = "{eventName:" + event + "," +
                "eventDate:" + eventDate + "," +
                "twitterTags:" + tags + "," +
                "hostUrl:" + hostUrl + "," +
                "accessToken:" + accessToken + "}";

            //make it POST ready
            formData = JSON.stringify(formData);

            $.post("/api/events", { '': formData })
            .success(function (data)
            {
                hideWait();

                if (data.errorMessage == null) {
                    alert("The event was added; you can find your new event site at " + data.newSiteUrl);
                    getEvents();
                }
                else
                    alert("Sorry, there was a problem adding the event: " + data.errorMessage);
            }).fail(function (errMsg)
            {
                hideWait();
                alert("Sorry, there was a problem: " + errMsg.responseText);
            });
        }

        function editItem(id) {

            //make sure there is still a value in twitter tags
            var tags = $("#" + id).val();

            if ((tags == undefined) || (tags == "")) {
                alert("The Twitter tags cannot be blank.");
                return;
            }

            showWait();

            //get the hiddens
            var hostUrl = $("#hdnHostWeb").val();
            var accessToken = $("#hdnAccessToken").val();

            //create the JSON string to post
            var formData = "{objectGraphID:" + id + "," + 
                "twitterTags:" + tags + "," + 
                "hostUrl:" + hostUrl + "," +
                "accessToken:" + accessToken + "}";

            //make it POST ready
            formData = JSON.stringify(formData);

            $.post("/api/events/updateevent", { '': formData })
            .success(function (data)
            {
                hideWait();

                if (data.success)
                    alert("Thanks, the Twitter tags for the event were updated.");
                else
                    alert("Sorry, there was a problem updating the Twitter tag: " + data.errorMessage);
            }).fail(function (errMsg) {
                hideWait();
                alert("Sorry was a problem updating your event: " + errMsg.responseText);
            });
        }

        function deleteItem(id) {

            if (confirm("Are you sure you want to delete the metadata for this site?  The site itself is not removed so you can still get your content.")) {

                showWait();

                //get the hiddens
                var hostUrl = $("#hdnHostWeb").val();
                var accessToken = $("#hdnAccessToken").val();

                //create the JSON string to post
                var formData = "{objectGraphID:" + id + "," +
                    "hostUrl:" + hostUrl + "," +
                    "accessToken:" + accessToken + "}";

                //make it POST ready
                formData = JSON.stringify(formData);

                $.post("/api/events/deleteevent", { '': formData })
                .success(function (data) {
                    if (data.success) {
                        hideWait();
                        alert("Thanks you, this event metadata was deleted but the site was not.  You can still access it in case you need to copy over any content.");
                        getEvents();
                    }
                    else {
                        hideWait();
                        alert("Sorry, there was a problem deleting the event metadata: " + data.errorMessage);
                    }

                }).fail(function (errMsg) {
                    hideWait();
                    alert("Sorry was a problem deleting your event metadata: " + errMsg.responseText);
                });
            }
        }

    </script>


    <form id="form1" runat="server">
        <div id="workingContent" style="display: none;">
            <asp:Literal runat="server" ID="hiddenLit"></asp:Literal>
        </div>

        <div>
            <asp:Label runat="server" ID="StatusLbl"></asp:Label>

            <br /><br />

            <div id="eventsMainDiv" class="eventsMainDiv">
                <div id="eventsDataDiv"></div>
                <p>
                    Create A New Event<br />
                    <img src="../images/EVENTS.PNG" title="Click to create a new event" onclick="showNewEvent();" class="eventActionLink" style="cursor:pointer;" />
                </p>
            </div>

            <div id="newEventDiv">

                <table style="width: 400px;">
                    <tr>
                        <td style="width: 100px;">Event Name:</td>
                        <td style="width: 300px;"><input type="text" id="eventNameTxt" name="eventNameTxt" style="width: 100%;" /></td>
                    </tr>
                    <tr>
                        <td>Event Date:</td>
                        <td><input type="text" id="eventDateTxt" name="eventDateTxt" class="eventDateTxt" style="width: 100%;" /></td>
                    </tr>
                    <tr>
                        <td>Twitter Tags:</td>
                        <td><input type="text" id="twitterTagsTxt" name="twitterTagsTxt" style="width: 100%;" /></td>
                    </tr>
                </table>

                <asp:Button runat="server" ID="AddEventBtn" Text="Add Event" OnClick="AddEventBtn_Click" Visible="false"/>             
                <input type="button" value="Add Event" onclick="addNewEvent();"  style="padding-right: 25px;"/>
                <input type="button" value="Cancel" onclick="hideNewEvent();"
            </div>

            <p></p>

            <div id="cleanUpDiv">
                <asp:Panel runat="server" ID="CleanUpPnl" Visible="false">
                    <asp:Button runat="server" ID="CleanUpBtn" Text="Clean Up App" OnClick="CleanUpBtn_Click"/>
                </asp:Panel>
            </div>

        </div>
    </form>

        <!-- Script to load SharePoint resources
        and load the blank.html page in
        the invisible iframe
        -->
    <script type="text/javascript">
        "use strict";
        var appweburl;

        (function () {
            var ctag;

            // Get the URI decoded app web URL.
            appweburl =
                decodeURIComponent(
                    getQueryStringParameter("SPAppWebUrl")
            );
            // Get the ctag from the SPClientTag token.
            ctag =
                decodeURIComponent(
                    getQueryStringParameter("SPClientTag")
            );

            // The resource files are in a URL in the form:
            // web_url/_layouts/15/Resource.ashx
            var scriptbase = appweburl + "/_layouts/15/";

            // Dynamically create the invisible iframe.
            var blankiframe;
            var blankurl;
            var body;
            blankurl = appweburl + "/Pages/blank.html";
            blankiframe = document.createElement("iframe");
            blankiframe.setAttribute("src", blankurl);
            blankiframe.setAttribute("style", "display: none");
            body = document.getElementsByTagName("body");
            body[0].appendChild(blankiframe);

            // Dynamically create the link element.
            var dclink;
            var head;
            dclink = document.createElement("link");
            dclink.setAttribute("rel", "stylesheet");
            dclink.setAttribute("href", scriptbase + "defaultcss.ashx?ctag=" + ctag);
            head = document.getElementsByTagName("head");
            head[0].appendChild(dclink);
        })();

        // Function to retrieve a query string value.
        // For production purposes you may want to use
        //  a library to handle the query string.
        function getQueryStringParameter(paramToRetrieve) {
            var params;
            var strParams;

            params = document.URL.split("?")[1].split("&");
            strParams = "";
            for (var i = 0; i < params.length; i = i + 1) {
                var singleParam = params[i].split("=");
                if (singleParam[0] == paramToRetrieve)
                    return singleParam[1];
            }
        }
    </script>

</body>
</html>
