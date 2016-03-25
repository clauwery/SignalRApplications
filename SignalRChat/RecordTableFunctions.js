function BroadcastMessage(name, message, value) {
    // Html encode display name and message.
    var encodedName = $('<div />').text(name).html();
    var encodedMsg = $('<div />').text(message).html();
    // Overwrite message
    $('#response').text(name + ': ' + message + ' = ' + value);
    // Add the message to the page.
    // $('#discussion').append('<li><strong>' + encodedName
    //  + '</strong>:&nbsp;&nbsp;' + encodedMsg + '</li>');
};


// http://jtable.org/ApiReference/Methods

// var container defined globally
function AddRecord(identifier) {
    $(container).jtable('addRecord', {
        record: {
            identifier: identifier,
            ip: "1.2.3.4",
            ping: "1",
            time: "OFFLINE"
        },
        clientOnly: true
    });
};

function DeleteRecord(identifier) {
    $(container).jtable('deleteRecord', {
        key: identifier,
        clientOnly: true
    });
};

function UpdateRecord(identifier, record) {
    record.identifier = identifier;
    $(container).jtable('updateRecord', {
        record: record,
        clientOnly: true
    });
};

/*
function IsRecord(identifier) {
    $(container).jtable('getRowByKey', {
        key: identifier
    });
};
*/
// UpdateRecord throws a warning when record to update does not exist
    // Catch, unfortunately, only catches errors ...
/*
function UpdateRecord(identifier, record) {
    record.identifier = identifier;
    try
    {
        $(container).jtable('updateRecord', {
            record: record,
            clientOnly: true
        });
    }
    catch(err)
    {
        AddRecord(identifier);
    }
};
*/